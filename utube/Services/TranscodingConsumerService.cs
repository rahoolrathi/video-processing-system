using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using utube.DTOs;
using utube.DTOs.utube.Dtos;
using utube.helper;
using utube.Repositories;

namespace utube.Services
{
    public class TranscodingConsumerService : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel; // ✅ use IChannel now
        private readonly IServiceProvider _serviceProvider;
public TranscodingConsumerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                HostName = "localhost",
              
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

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var message = JsonConvert.DeserializeObject<TranscodingJobMessageDto>(json);

               Console.WriteLine($"[Consumer] Received job: VideoId={message.VideoId}, Path={message.VideoPath}");

                var profileDto = await GetProfileWithFormats(message.EncodingProfileId);  // likely error here

                // ///Console.WriteLine($"[Consumer] Profile: {profileDto.ProfileName}, Resolutions: {profileDto.Resolutions} BitratesKbps: {profileDto.BitratesKbps}");

                //foreach (var format in profileDto.Formats)
                //{
                //    Console.WriteLine($"  → Format: {format.FormatType}");
                //}
                Console.WriteLine($"Resolution : {profileDto.Resolutions}, Bitrate: {profileDto.BitratesKbps}");


                string outputDir = Path.Combine("Transcoded", message.VideoId.ToString());
                var transcoder = new FfmpegTranscoder();

                await transcoder.TranscodeToCmafAsync(message.VideoPath, outputDir, profileDto);

                Console.WriteLine($"[Consumer] Transcoding complete: {outputDir}");

                await _channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // Optional: negatively acknowledge if needed
                // await _channel.BasicNackAsync(args.DeliveryTag, false, true);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }

        public async Task<EncodingProfileWithFormatsDto> GetProfileWithFormats(Guid profileId)
        {
            using var scope = _serviceProvider.CreateScope();
            var encodingProfileRepository = scope.ServiceProvider.GetRequiredService<IEncodingProfileRepository>();

            var profile = await encodingProfileRepository.GetByIdAsync(profileId);

            return new EncodingProfileWithFormatsDto
            {
                Id = profile.Id,
                ProfileName = profile.ProfileName,
                Resolutions = profile.Resolutions,
                BitratesKbps = profile.BitratesKbps,
                Formats = profile.Formats.Select(f => new FormatDto
                {
                    Id = f.Id,
                    FormatType = f.FormatType,
                }).ToList()
            };
        }


    }
}
