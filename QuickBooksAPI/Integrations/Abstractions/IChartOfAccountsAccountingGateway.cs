namespace QuickBooksAPI.Integrations.Abstractions;

/// <summary>
/// Pulls chart-of-accounts JSON pages from the connected accounting provider.
/// </summary>
public interface IChartOfAccountsAccountingGateway
{
    Task<string> FetchAccountsPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default);
}
