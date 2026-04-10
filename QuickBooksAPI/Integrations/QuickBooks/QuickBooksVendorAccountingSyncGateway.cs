using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Integrations.QuickBooks;

internal sealed class QuickBooksVendorAccountingSyncGateway : IVendorAccountingSyncGateway
{
    private readonly IQuickBooksVendorService _quickBooksVendorService;

    public QuickBooksVendorAccountingSyncGateway(IQuickBooksVendorService quickBooksVendorService)
    {
        _quickBooksVendorService = quickBooksVendorService ?? throw new ArgumentNullException(nameof(quickBooksVendorService));
    }

    public Task<string> FetchVendorsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default)
    {
        return _quickBooksVendorService.GetVendorsAsync(
            accessToken,
            realmId,
            startPosition,
            pageSize,
            incrementalSinceUtc);
    }
}
