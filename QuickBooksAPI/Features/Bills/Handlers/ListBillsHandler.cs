using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Bills.Handlers;

public sealed class ListBillsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IBillReadService _read;

    public ListBillsHandler(IRequestContext requestContext, IBillReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<IEnumerable<BillListItemDto>>> HandleListAsync()
    {
        if (!FeatureRequestContextGuard.TryGetRealm(_requestContext, out var realmId, out var err))
            return ApiResponse<IEnumerable<BillListItemDto>>.Fail(err!);
        return await _read.ListAsync(realmId);
    }

    public async Task<ApiResponse<PagedResult<BillListItemDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (!FeatureRequestContextGuard.TryGetRealm(_requestContext, out var realmId, out var err))
            return ApiResponse<PagedResult<BillListItemDto>>.Fail(err!);
        return await _read.ListPagedAsync(realmId, query);
    }

    public async Task<ApiResponse<BillListItemDto>> HandleGetByIdAsync(string id)
    {
        if (!FeatureRequestContextGuard.TryGetRealm(_requestContext, out var realmId, out var err))
            return ApiResponse<BillListItemDto>.Fail(err!);
        return await _read.GetByIdAsync(realmId, id);
    }
}
