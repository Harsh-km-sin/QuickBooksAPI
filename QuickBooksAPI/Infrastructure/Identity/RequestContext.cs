using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure.Identity;

/// <summary>
/// Scoped request context; mutated only by middleware.
/// </summary>
public sealed class RequestContext : IRequestContext
{
    public string? UserId { get; internal set; }

    public string? RealmId { get; internal set; }

    public string? CorrelationId { get; internal set; }
}
