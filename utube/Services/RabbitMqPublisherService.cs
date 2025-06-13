using Microsoft.Identity.Client;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace utube.Services
{
    public interface IRabbitMqPublisherService
    {
        Task Publish<T>(string queueName, T message);
        Task InitializeAsync();
    }

    public class RabbitMqPublisherService : IRabbitMqPublisherService
    {
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMqPublisherService()
        {
            InitializeAsync(); // <-- Sync wrapper for async init
        }



        public async Task InitializeAsync()
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
            await _channel.BasicPublishAsync(exchange:"", routingKey: queueName,
     mandatory: true, basicProperties: props, body: body);
        }
    }
}
