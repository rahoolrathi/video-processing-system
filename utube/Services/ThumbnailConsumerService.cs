using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using utube.DTOs;
using utube.Enums;
using utube.Options;
using utube.Repositories;

namespace utube.Services
{
    public class ThumbnailConsumerService : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _rabbitOptions;

        public ThumbnailConsumerService(IServiceProvider serviceProvider, IOptions<RabbitMqOptions> rabbitOptions)
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
                    queue: "thumbnail-generation-queue",
                    autoAck: false,
                    consumer: consumer
                );

                Console.WriteLine("[ThumbnailConsumer] Listening to thumbnail-generation-queue...");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[ThumbnailConsumer] Service cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThumbnailConsumer] Fatal error: {ex.Message}");
                throw;
            }
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IThumbnailJobRepository>();
            var uploader = scope.ServiceProvider.GetRequiredService<AzureBlobUploader>();

            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var jobMsg = JsonConvert.DeserializeObject<ThumbnailJobMessageDto>(json);

                if (jobMsg == null)
                    throw new Exception("Invalid message format");

                Console.WriteLine($"[Thumbnail] >>> START processing job {jobMsg.JobId} for video {jobMsg.VideoId}");

                var thumbnailsDir = Path.Combine("thumbnails", jobMsg.VideoId.ToString());
                Directory.CreateDirectory(thumbnailsDir);

                int interval = 5;

                string ffmpegArgs = $"-i \"{jobMsg.VideoPath}\" -vf \"fps=1/{interval}\" -qscale:v 2 \"{thumbnailsDir}/thumb_%03d.jpg\"";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = ffmpegArgs,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Start draining output and error buffers
                _ = Task.Run(() => ReadStreamAsync(process.StandardOutput, "[FFmpeg][OUT]"));
                _ = Task.Run(() => ReadStreamAsync(process.StandardError, "[FFmpeg][ERR]"));

                await process.WaitForExitAsync();


                var files = Directory.GetFiles(thumbnailsDir, "thumb_*.jpg");
                int imageCount = files.Length;

                // Upload to Azure Blob Storage
                string remoteFolder = $"thumbnails/{jobMsg.VideoId}";
                await uploader.UploadFolderAsync(thumbnailsDir, remoteFolder);

                // Delete local folder
                Directory.Delete(thumbnailsDir, recursive: true);

                // Update DB status
                var job = await repo.GetByIdAsync(jobMsg.JobId);
                if (job != null)
                {
                    job.NumberOfImages = imageCount;
                    job.FilePath = remoteFolder; // Now this is blob path
                    job.Status = JobStatus.Done;

                    await repo.UpdateAsync(job);
                }


                await _channel.BasicAckAsync(args.DeliveryTag, false);

                Console.WriteLine($"[Thumbnail] <<< DONE processing job {jobMsg.JobId}: Uploaded {imageCount} thumbnails to Azure.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Thumbnail] ERROR: " + ex.Message);

                try
                {
                    var jobMsg = JsonConvert.DeserializeObject<ThumbnailJobMessageDto>(Encoding.UTF8.GetString(args.Body.ToArray()));
                    if (jobMsg?.JobId != null)
                    {
                        var fallbackRepo = _serviceProvider.GetRequiredService<IThumbnailJobRepository>();
                        var job = await fallbackRepo.GetByIdAsync(jobMsg.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Error;
                            await fallbackRepo.UpdateAsync(job);
                        }
                    }
                }
                catch { /* suppress inner fallback errors */ }

                await _channel.BasicAckAsync(args.DeliveryTag, false);
            }
        }


        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
        private async Task ReadStreamAsync(StreamReader reader, string prefix)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"{prefix} {line}");
            }
        }

    }
}
