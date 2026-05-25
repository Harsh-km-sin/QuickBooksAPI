using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Customers.Handlers;

public sealed class ListCustomersHandler
{
    private readonly IRequestContext _requestContext;
    private readonly ICustomerReadService _read;

    public ListCustomersHandler(IRequestContext requestContext, ICustomerReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<IEnumerable<CustomerDto>>> HandleListAsync()
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<IEnumerable<CustomerDto>>.Fail(err!);
        return await _read.ListAsync(userId, realmId);
    }

    public async Task<ApiResponse<PagedResult<CustomerDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<PagedResult<CustomerDto>>.Fail(err!);
        return await _read.ListPagedAsync(userId, realmId, query);
    }

    public async Task<ApiResponse<CustomerDto>> HandleGetByIdAsync(string id)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<CustomerDto>.Fail(err!);
        return await _read.GetByQboIdAsync(userId, realmId, id);
    }
}
