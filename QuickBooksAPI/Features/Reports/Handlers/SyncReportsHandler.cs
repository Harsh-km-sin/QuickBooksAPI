using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Reports.Handlers;

public sealed class SyncReportsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IReportQboSyncService _sync;

    public SyncReportsHandler(IRequestContext requestContext, IReportQboSyncService sync)
    {
        _requestContext = requestContext;
        _sync = sync;
    }

    /// <param name="force">
    /// True for the manual "Generate Reports" action; false for the Full Sync step, which skips the
    /// pull when no financial entity has changed.
    /// </param>
    public async Task<ApiResponse<int>> HandleAsync(bool force)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId, force);
    }
}
