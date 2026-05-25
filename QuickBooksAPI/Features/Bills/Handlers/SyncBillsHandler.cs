using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Bills.Handlers;

public sealed class SyncBillsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IBillQboSyncService _sync;

    public SyncBillsHandler(IRequestContext requestContext, IBillQboSyncService sync)
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
