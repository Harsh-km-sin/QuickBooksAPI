using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// Invokes <see cref="IFullSyncCompletedSubscriber"/> hooks after a full sync run (isolated failure boundary per subscriber).
/// </summary>
public sealed class FullSyncCompletionDispatcher
{
    private readonly ILogger<FullSyncCompletionDispatcher> _logger;

    public FullSyncCompletionDispatcher(ILogger<FullSyncCompletionDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(
        IReadOnlyList<IFullSyncCompletedSubscriber> subscribers,
        FullSyncCompletionContext context,
        string companyIdForLogging,
        CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in subscribers)
        {
            try
            {
                await subscriber.OnFullSyncCompletedAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception subEx)
            {
                _logger.LogWarning(subEx, "IFullSyncCompletedSubscriber failed for CompanyId={CompanyId}", companyIdForLogging);
            }
        }
    }
}
