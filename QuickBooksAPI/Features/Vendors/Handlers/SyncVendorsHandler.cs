using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Vendors.Handlers;

public sealed class SyncVendorsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IVendorQboSyncService _sync;

    public SyncVendorsHandler(IRequestContext requestContext, IVendorQboSyncService sync)
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
