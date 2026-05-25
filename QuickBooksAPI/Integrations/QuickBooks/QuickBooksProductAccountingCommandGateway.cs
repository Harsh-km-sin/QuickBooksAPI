using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Integrations.QuickBooks;

internal sealed class QuickBooksProductAccountingCommandGateway : IProductAccountingCommandGateway
{
    private readonly IQuickBooksProductService _quickBooksProductService;

    public QuickBooksProductAccountingCommandGateway(IQuickBooksProductService quickBooksProductService)
    {
        _quickBooksProductService = quickBooksProductService ?? throw new ArgumentNullException(nameof(quickBooksProductService));
    }

    public Task<string> CreateProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default) =>
        _quickBooksProductService.CreateProductAsync(accessToken, realmId, productPayload);

    public Task<string> UpdateProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default) =>
        _quickBooksProductService.UpdateProductAsync(accessToken, realmId, productPayload);

    public Task<string> DeleteProductAsync(
        string accessToken,
        string realmId,
        string productPayload,
        CancellationToken cancellationToken = default) =>
        _quickBooksProductService.DeleteProductAsync(accessToken, realmId, productPayload);
}
