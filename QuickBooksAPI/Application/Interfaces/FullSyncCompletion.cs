namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// Published after a full sync run finishes (success, partial failure, or completion after errors were recorded).
/// </summary>
public sealed record FullSyncCompletionContext(
    string RealmId,
    string UserId,
    string CorrelationId,
    bool Succeeded,
    IReadOnlyDictionary<string, int> EntityCounts,
    IReadOnlyList<string> Errors);

/// <summary>
/// Optional hook for logging, cache invalidation, or downstream notifications without coupling orchestrator to concrete consumers.
/// </summary>
public interface IFullSyncCompletedSubscriber
{
    Task OnFullSyncCompletedAsync(FullSyncCompletionContext context, CancellationToken cancellationToken = default);
}
