namespace QuickBooksAPI.Infrastructure.Queue
{
    public class NoOpQueuePublisher : IQueuePublisher
    {
        public Task PublishAsync<T>(T message)
        {
            throw new InvalidOperationException(
                "Service Bus is not configured. Add 'ServiceBus:ConnectionString' to your configuration (user secrets, appsettings, or environment variables).");
        }
    }
}
