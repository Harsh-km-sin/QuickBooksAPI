namespace QuickBooksAPI.Application.Interfaces;

public interface IGlQueuePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
}
