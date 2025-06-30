using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using utube.DTOs;
using utube.DTOs.utube.Dtos;
using utube.Enums;
using utube.helper;
using utube.Models;
using utube.Options;
using utube.Repositories;

namespace utube.Services
{
    public class TranscodingConsumerService : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _rabbitOptions;


        public TranscodingConsumerService(IServiceProvider serviceProvider, IOptions<RabbitMqOptions> rabbitOptions)
        {
            _serviceProvider = serviceProvider;
            _rabbitOptions = rabbitOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    UserName = _rabbitOptions.UserName,
                    Password = _rabbitOptions.Password,
                    VirtualHost = _rabbitOptions.VirtualHost,
                    HostName = _rabbitOptions.HostName
                };


                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(
                    queue: "transcoding-queue",
                    autoAck: false,
                    consumer: consumer
                );

                Console.WriteLine("[Transcoder] Waiting for messages on transcoding-queue...");

                // Graceful shutdown wait
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[Transcoder] Background service cancelled. Shutting down cleanly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transcoder] Fatal error: {ex.Message}");
                throw; // Let host handle it (optional based on your policy)
            }
        }


        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
        {
            string workerId = Environment.MachineName;

            try
            {
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<TranscodingJobMessageDto>(json);
                Console.WriteLine("[Consumer] Received JSON: " + json);

                if (message == null)
                {
                    Console.WriteLine("[Consumer] ERROR: Received null message.");
                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var transcodeJobRepository = scope.ServiceProvider.GetRequiredService<ITranscodeJobRepository>();
                var encodingProfileRepository = scope.ServiceProvider.GetRequiredService<IEncodingProfileRepository>();
                var cloudServiceProvider = scope.ServiceProvider.GetRequiredService<AzureBlobUploader>();
                var elasticSearchService = scope.ServiceProvider.GetRequiredService<ElasticSearchService>();
                var _iVideorepo = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
                // Step 1: Mark job as Processing
                var job = await transcodeJobRepository.GetByVideoAndProfileAsync(message.VideoId, message.EncodingProfileId);
                if (job != null)
                {
                    job.Status = JobStatus.Processing;
                    job.WorkerId = workerId;
                    await transcodeJobRepository.UpdateAsync(job);
                }

                Console.WriteLine($"[Consumer] Started processing VideoId={message.VideoId}");

                // Step 2: Load profile
                var profile = await encodingProfileRepository.GetByIdAsync(message.EncodingProfileId);
                if (profile == null) throw new Exception("Encoding profile not found.");

                var profileDto = new EncodingProfileWithFormatsDto
                {
                    Id = profile.Id,
                    ProfileName = profile.ProfileName,
                    Resolutions = profile.Resolutions,
                    BitratesKbps = profile.BitratesKbps,
                    Formats = profile.Formats.Select(f => new FormatDto
                    {
                        Id = f.Id,
                        FormatType = f.FormatType
                    }).ToList()
                };

                var videoId = message.VideoId.ToString();
                string outputDir = Path.Combine("transcoded", videoId); // lowercase "transcoded" folder
                Directory.CreateDirectory(outputDir);
                var remotePath = $"transcoded/{message.VideoId}";
                var transcoder = new FfmpegTranscoder();

                // Step 3: Transcode
                try
                {
                    Console.WriteLine("[Consumer] Transcoding started...");
                    await transcoder.TranscodeToCmafAsync(
                        message.VideoPath,
                        outputDir,
                        profileDto,
                        message.EncryptionKey,
                        message.KeyId
                    );

                    // Step 4: Upload to Azure
                    await cloudServiceProvider.UploadFolderAsync(outputDir, remotePath);
                    Console.WriteLine("[Consumer] Upload complete.");

                    // Optional cleanup
                    Directory.Delete(outputDir, recursive: true);
                    Console.WriteLine($"[Consumer] Deleted local folder: {outputDir}");

                    // Update the code to ensure the correct namespace is used for FormatType
                    var allFormats = profile.Formats.Select(f => (utube.Enums.FormatType)f.FormatType).ToList();

                    // ✅ Just store the raw remote path and all formats
                    await elasticSearchService.UpdateSelectedThumbnailAsync(message.VideoId, remotePath, allFormats);

                    Console.WriteLine("[Consumer] Updated Elasticsearch with raw path and format list.");




                    Console.WriteLine("[Consumer] Indexed video into Elasticsearch.");
                }
                catch (Exception transcodeEx)
                {
                    Console.WriteLine($"[Consumer] Transcode ERROR: {transcodeEx.Message}");
                    throw;
                }

                // Step 5: Mark job as Done
                if (job != null)
                {
                    job.Status = JobStatus.Done;
                    job.TranscodedPath = remotePath;
                    await transcodeJobRepository.UpdateAsync(job);
                }

                await _channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] ERROR: {ex.Message}");

                // Attempt to mark as error in DB
                try
                {
                    var body = args.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TranscodingJobMessageDto>(json);

                    if (message != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var transcodeJobRepository = scope.ServiceProvider.GetRequiredService<ITranscodeJobRepository>();

                        var job = await transcodeJobRepository.GetByVideoAndProfileAsync(message.VideoId, message.EncodingProfileId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Error;
                            job.WorkerId ??= workerId;
                            job.ErrorMessage = ex.Message;
                            await transcodeJobRepository.UpdateAsync(job);
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"[Consumer] Failed to mark DB error: {dbEx.Message}");
                }

                await _channel.BasicAckAsync(args.DeliveryTag, false);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
