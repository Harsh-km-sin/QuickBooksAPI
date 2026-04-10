using QuickBooksShared.Messages;

namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// Runs the full QBO → DB sync pipeline for a single company (e.g. from a Service Bus message).
/// </summary>
public interface IFullSyncOrchestrator
{
    /// <summary>
    /// Executes entity syncs, warehouse rebuild, and anomaly detection. Does not complete the Service Bus message.
    /// </summary>
    Task<FullSyncOrchestrationResult> RunAsync(FullSyncMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a full sync run for status reporting and logging.
/// </summary>
public sealed class FullSyncOrchestrationResult
{
    public required IReadOnlyDictionary<string, int> EntityCounts { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public bool HasPartialFailure => Errors.Count > 0;
}
