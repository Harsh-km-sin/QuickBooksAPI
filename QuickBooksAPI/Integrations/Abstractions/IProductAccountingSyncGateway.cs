namespace QuickBooksAPI.Integrations.Abstractions;

/// <summary>
/// Pulls product/item query JSON from the connected accounting provider (QuickBooks Online adapter in <c>Integrations/QuickBooks</c>).
/// </summary>
public interface IProductAccountingSyncGateway
{
    /// <summary>
    /// Returns JSON for one paged Item query (caller deserializes with QuickBooks DTOs).
    /// </summary>
    Task<string> FetchProductsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default);
}
