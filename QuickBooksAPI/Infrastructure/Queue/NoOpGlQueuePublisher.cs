using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure.Queue;

/// <summary>Fallback when GL Service Bus queue is not configured. Logs a warning and does nothing.</summary>
public class NoOpGlQueuePublisher : IGlQueuePublisher
{
    private readonly ILogger<NoOpGlQueuePublisher> _logger;

    public NoOpGlQueuePublisher(ILogger<NoOpGlQueuePublisher> logger) => _logger = logger;

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GL analysis queue not configured. Message of type {Type} was not published.", typeof(T).Name);
        return Task.CompletedTask;
    }
}
