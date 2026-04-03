namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// Per-scope context for background workers. Populated from the queue message before scoped services run.
/// </summary>
public interface ISyncContext
{
    string? UserId { get; }

    string? RealmId { get; }

    /// <summary>Optional id from the message, or generated per run for tracing.</summary>
    string? CorrelationId { get; }
}
