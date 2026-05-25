using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.ChartOfAccounts.Handlers;

public sealed class ListChartOfAccountsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IChartOfAccountsRepository _repository;

    public ListChartOfAccountsHandler(IRequestContext requestContext, IChartOfAccountsRepository repository)
    {
        _requestContext = requestContext;
        _repository = repository;
    }

    public async Task<ApiResponse<IEnumerable<ChartOfAccountsItemDto>>> HandleListAsync()
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<IEnumerable<ChartOfAccountsItemDto>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var userId = int.Parse(_requestContext.UserId);
        var realmId = _requestContext.RealmId;
        var rows = await _repository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<ChartOfAccountsItemDto>>.Ok(rows);
    }

    public async Task<ApiResponse<PagedResult<ChartOfAccountsItemDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<PagedResult<ChartOfAccountsItemDto>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var userId = int.Parse(_requestContext.UserId);
        var realmId = _requestContext.RealmId;
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _repository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search);
        return ApiResponse<PagedResult<ChartOfAccountsItemDto>>.Ok(result);
    }
}
