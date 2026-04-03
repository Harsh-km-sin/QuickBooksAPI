using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// Default subscriber: structured log line for traceability (register additional <c>IFullSyncCompletedSubscriber</c> implementations as needed).
/// </summary>
public sealed class LoggingFullSyncCompletedSubscriber : IFullSyncCompletedSubscriber
{
    private readonly ILogger<LoggingFullSyncCompletedSubscriber> _logger;

    public LoggingFullSyncCompletedSubscriber(ILogger<LoggingFullSyncCompletedSubscriber> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task OnFullSyncCompletedAsync(FullSyncCompletionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Full sync completed RealmId={RealmId} UserId={UserId} CorrelationId={CorrelationId} Succeeded={Succeeded} EntityCount={EntityCount} ErrorCount={ErrorCount}",
            context.RealmId,
            context.UserId,
            context.CorrelationId,
            context.Succeeded,
            context.EntityCounts.Count,
            context.Errors.Count);
        return Task.CompletedTask;
    }
}
