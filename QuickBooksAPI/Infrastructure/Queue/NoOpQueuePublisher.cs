using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Infrastructure.Queue
{
    public class NoOpQueuePublisher : IQueuePublisher
    {
        private readonly ILogger<NoOpQueuePublisher> _logger;

        public NoOpQueuePublisher(ILogger<NoOpQueuePublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T message)
        {
            _logger.LogWarning("ServiceBus not configured — message of type {Type} was dropped. To run sync locally, start the SyncWorker and configure ServiceBus:ConnectionString.", typeof(T).Name);
            return Task.CompletedTask;
        }
    }
}
