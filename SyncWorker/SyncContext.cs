using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// Scoped sync context for Service Bus handlers; set from the message at the start of orchestration.
/// Also exposed as <see cref="IRequestContext"/> so shared services (e.g. <c>CustomerService</c>) resolve in the worker host.
/// </summary>
public sealed class SyncContext : ISyncContext, IRequestContext
{
    public string? UserId { get; set; }

    public string? RealmId { get; set; }

    public string? CorrelationId { get; set; }
}
