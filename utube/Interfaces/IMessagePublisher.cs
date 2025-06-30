namespace utube.Interfaces
{
    public interface IMessagePublisher
    {
        Task Publish<T>(string topicOrQueueName, T message);
    }

}
