using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Reports.Handlers;

public sealed class ListReportPeriodsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IReportReadService _read;

    public ListReportPeriodsHandler(IRequestContext requestContext, IReportReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<IEnumerable<ReportPeriodDto>>> HandleAsync(string reportType)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<IEnumerable<ReportPeriodDto>>.Fail(err!);

        return await _read.GetSyncedPeriodsAsync(userId, realmId, reportType);
    }
}
