using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using utube.DTOs;
using utube.Enums;
using utube.Repositories;

namespace utube.Services
{
    public class WatermarkingConsumer : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly IServiceProvider _serviceProvider;

        public WatermarkingConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    UserName = "guest",
                    Password = "guest",
                    VirtualHost = "/",
                    HostName = "localhost"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(
                    queue: "Watermarking-queue",
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken
                );

                Console.WriteLine("[WatermarkingConsumer] Listening to watermarking-queue...");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WatermarkingConsumer] Fatal error: {ex.Message}");
            }
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Body.ToArray());
                Console.WriteLine($"[WatermarkingConsumer] Received message: {message}");

                var watermarkingRequest = JsonConvert.DeserializeObject<WatermarkingJobDto>(message);
                if (watermarkingRequest == null)
                {
                    Console.WriteLine("[WatermarkingConsumer] Invalid message format.");
                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var _watermarkingRepository = scope.ServiceProvider.GetRequiredService<IWatermarkingRepository>();
                var azureUploader = scope.ServiceProvider.GetRequiredService<AzureBlobUploader>();
                await _watermarkingRepository.UpdateStatusAsync(watermarkingRequest.JobId, JobStatus.Processing);

                var inputPath = watermarkingRequest.VideoPath;
                var videoId = watermarkingRequest.VideoId.ToString();
                var text = watermarkingRequest.Text;
                var inputFileName = Path.GetFileName(inputPath);
                var outputFolder = Path.Combine("watermarkingoutput", videoId);
                Directory.CreateDirectory(outputFolder);

                var outputFile = Path.Combine(outputFolder, inputFileName);

                var arguments = $"-i \"{inputPath}\" -vf \"drawtext=fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5:boxborderw=5:text='{text}':x=10:y=H-th-10\" -codec:a copy \"{outputFile}\"";

                Console.WriteLine($"[WatermarkingConsumer] Running FFmpeg:\nffmpeg {arguments}");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[WatermarkingConsumer] FFmpeg failed:\n{stderr}");
                    await _watermarkingRepository.UpdateStatusAsync(watermarkingRequest.JobId, JobStatus.Error);
                }
                else
                {
                    try
                    {
                        Console.WriteLine("[WatermarkingConsumer] Watermarking completed successfully.");

                        // Upload to Azure

                        var localFolder = Path.GetDirectoryName(outputFile)!;
                        var remoteFolder = $"watermarkingvideo/{videoId}";

                        await azureUploader.UploadFolderAsync(localFolder, remoteFolder);

                        // Get the final uploaded blob path (use relative path)
                        var uploadedBlobPath = $"{remoteFolder}/{Path.GetFileName(outputFile)}";

                        await _watermarkingRepository.UpdatePathAndStatusAsync(
                            watermarkingRequest.JobId,
                            uploadedBlobPath,
                            JobStatus.Done
                        );

                        Console.WriteLine($"[WatermarkingConsumer] Uploaded to Azure: {uploadedBlobPath}");
                    }
                    catch (Exception uploadEx)
                    {
                        Console.WriteLine($"[WatermarkingConsumer] Error during upload or DB update: {uploadEx.Message}");

                        // Mark job as failed in DB
                        await _watermarkingRepository.UpdatePathAndStatusAsync(
                            watermarkingRequest.JobId,
                            null,
                            JobStatus.Error
                        );
                    }
                }

            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[WatermarkingConsumer] JSON error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WatermarkingConsumer] Unexpected error: {ex.Message}");
            }
            finally
            {
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
