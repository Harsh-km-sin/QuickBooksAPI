namespace QuickBooksAPI.Integrations.Abstractions;

/// <summary>
/// Create/update/delete Items in the connected accounting provider (QuickBooks Online adapter in <c>Integrations/QuickBooks</c>).
/// </summary>
public interface IProductAccountingCommandGateway
{
    Task<string> CreateProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default);

    Task<string> UpdateProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default);

    Task<string> DeleteProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default);
}
