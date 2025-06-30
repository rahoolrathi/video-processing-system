using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using utube.Interfaces;
using utube.Options;

namespace utube.Services
{
   

    public class RabbitMqPublisherService :  IMessagePublisher
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly RabbitMqOptions _options;

        public RabbitMqPublisherService(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
            InitializeAsync(); // <-- Sync wrapper for async init
        }



        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }

        public async Task Publish<T>(string queueName, T message)
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel not initialized. Call InitializeAsync() first.");

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            var props = new BasicProperties();
            await _channel.BasicPublishAsync(exchange: "", routingKey: queueName,
     mandatory: true, basicProperties: props, body: body);
        }
    }
}
