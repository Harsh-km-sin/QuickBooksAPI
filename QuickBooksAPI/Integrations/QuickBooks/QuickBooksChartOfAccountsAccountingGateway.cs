using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Integrations.QuickBooks;

internal sealed class QuickBooksChartOfAccountsAccountingGateway : IChartOfAccountsAccountingGateway
{
    private readonly IQuickBooksChartOfAccountsService _chartOfAccountsService;

    public QuickBooksChartOfAccountsAccountingGateway(IQuickBooksChartOfAccountsService chartOfAccountsService)
    {
        _chartOfAccountsService = chartOfAccountsService ?? throw new ArgumentNullException(nameof(chartOfAccountsService));
    }

    public Task<string> FetchAccountsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default) =>
        _chartOfAccountsService.GetChartOfAccountsAsync(accessToken, realmId, startPosition, pageSize, incrementalSinceUtc);
}
