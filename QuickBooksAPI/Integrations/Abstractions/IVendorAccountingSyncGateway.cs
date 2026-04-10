namespace QuickBooksAPI.Integrations.Abstractions;

/// <summary>
/// Pulls vendor data from the connected accounting provider (QuickBooks Online adapter in <c>Integrations/QuickBooks</c>).
/// </summary>
public interface IVendorAccountingSyncGateway
{
    /// <summary>
    /// Returns a JSON document for one paged vendor query (shape is provider-specific; callers deserialize with their DTOs).
    /// </summary>
    Task<string> FetchVendorsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default);
}
