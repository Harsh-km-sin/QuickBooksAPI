using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Customers.Handlers;

public sealed class SyncCustomersHandler
{
    private readonly IRequestContext _requestContext;
    private readonly ICustomerQboSyncService _sync;

    public SyncCustomersHandler(IRequestContext requestContext, ICustomerQboSyncService sync)
    {
        _requestContext = requestContext;
        _sync = sync;
    }

    public async Task<ApiResponse<int>> HandleAsync()
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId);
    }
}
