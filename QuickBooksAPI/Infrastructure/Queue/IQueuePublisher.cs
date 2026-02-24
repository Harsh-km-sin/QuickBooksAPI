namespace QuickBooksAPI.Infrastructure.Queue
{
    public interface IQueuePublisher
    {
        Task PublishAsync<T>(T message);
    }
}
