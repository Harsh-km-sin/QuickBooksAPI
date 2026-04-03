namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// Per-request identity and tracing for the HTTP pipeline. Populated by middleware after authentication; correlation id aligns with <c>X-Correlation-Id</c> / request items.
/// </summary>
public interface IRequestContext
{
    string? UserId { get; }

    string? RealmId { get; }

    /// <summary>Correlation id for this request (header or generated).</summary>
    string? CorrelationId { get; }
}
