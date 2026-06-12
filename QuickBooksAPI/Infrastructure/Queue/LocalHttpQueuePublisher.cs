using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace QuickBooksAPI.Infrastructure.Queue;

public class LocalHttpQueuePublisher : IQueuePublisher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _syncWorkerUrl;
    private readonly ILogger<LocalHttpQueuePublisher> _logger;

    public LocalHttpQueuePublisher(IHttpClientFactory httpClientFactory, string syncWorkerUrl, ILogger<LocalHttpQueuePublisher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _syncWorkerUrl = syncWorkerUrl;
        _logger = logger;
    }

    public Task PublishAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        _logger.LogInformation("Dispatching sync message to local SyncWorker at {Url}", _syncWorkerUrl);

        // Fire-and-forget — mirrors real Service Bus behaviour (enqueue returns immediately).
        _ = client.PostAsync(_syncWorkerUrl, new StringContent(json, Encoding.UTF8, "application/json"))
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Local SyncWorker HTTP call failed");
                else if (t.Result is { IsSuccessStatusCode: false } r)
                    _logger.LogError("Local SyncWorker returned {Status}", (int)r.StatusCode);
            }, TaskScheduler.Default);

        return Task.CompletedTask;
    }
}
