using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Vendors.Handlers;

public sealed class ListVendorsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IVendorReadService _read;

    public ListVendorsHandler(IRequestContext requestContext, IVendorReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<IEnumerable<VendorDto>>> HandleListAsync()
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<IEnumerable<VendorDto>>.Fail(err!);
        return await _read.ListAsync(userId, realmId);
    }

    public async Task<ApiResponse<PagedResult<VendorDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<PagedResult<VendorDto>>.Fail(err!);
        return await _read.ListPagedAsync(userId, realmId, query);
    }
}
