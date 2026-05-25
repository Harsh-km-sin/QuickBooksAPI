using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.ChartOfAccounts.Handlers;

namespace QuickBooksAPI.Features.ChartOfAccounts;

/// <summary>
/// MIGRATION-ONLY: delegates to chart-of-accounts feature handlers.
/// </summary>
public sealed class ChartOfAccountsServiceMigrationFacade : IChartOfAccountsService
{
    private readonly ListChartOfAccountsHandler _list;
    private readonly SyncChartOfAccountsHandler _sync;

    public ChartOfAccountsServiceMigrationFacade(
        ListChartOfAccountsHandler list,
        SyncChartOfAccountsHandler sync)
    {
        _list = list;
        _sync = sync;
    }

    public Task<ApiResponse<IEnumerable<ChartOfAccountsItemDto>>> ListChartOfAccountsAsync() =>
        _list.HandleListAsync();

    public Task<ApiResponse<PagedResult<ChartOfAccountsItemDto>>> ListChartOfAccountsAsync(ListQueryParams query) =>
        _list.HandlePagedAsync(query);

    public Task<ApiResponse<int>> syncChartOfAccounts() => _sync.HandleAsync();
}
