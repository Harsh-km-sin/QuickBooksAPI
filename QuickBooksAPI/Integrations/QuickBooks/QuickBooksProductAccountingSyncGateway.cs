using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Integrations.QuickBooks;

internal sealed class QuickBooksProductAccountingSyncGateway : IProductAccountingSyncGateway
{
    private readonly IQuickBooksProductService _quickBooksProductService;

    public QuickBooksProductAccountingSyncGateway(IQuickBooksProductService quickBooksProductService)
    {
        _quickBooksProductService = quickBooksProductService ?? throw new ArgumentNullException(nameof(quickBooksProductService));
    }

    public Task<string> FetchProductsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default)
    {
        return _quickBooksProductService.GetProductsAsync(
            accessToken,
            realmId,
            startPosition,
            pageSize,
            incrementalSinceUtc);
    }
}
