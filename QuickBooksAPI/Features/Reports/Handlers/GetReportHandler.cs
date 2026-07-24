using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Reports.Handlers;

public sealed class GetReportHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IReportReadService _read;

    public GetReportHandler(IRequestContext requestContext, IReportReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<ReportTreeDto>> HandleProfitAndLossAsync(
        DateTime? startDate,
        DateTime? endDate,
        bool useFiscalYear,
        AccountingMethod? accountingMethod = null)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<ReportTreeDto>.Fail(err!);

        // "Use fiscal year" resolves its boundaries from the company's stored FiscalYearStartMonth,
        // so the filter is served entirely from our DB with no QBO call.
        if (useFiscalYear)
            return await _read.GetProfitAndLossForFiscalYearAsync(userId, realmId, endDate ?? DateTime.UtcNow.Date, accountingMethod);

        if (startDate is null || endDate is null)
            return ApiResponse<ReportTreeDto>.Fail("startDate and endDate are required unless useFiscalYear is true.");

        return await _read.GetProfitAndLossAsync(userId, realmId, startDate.Value, endDate.Value, accountingMethod);
    }

    public async Task<ApiResponse<ReportTreeDto>> HandleBalanceSheetAsync(
        DateTime? asOfDate,
        AccountingMethod? accountingMethod = null)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<ReportTreeDto>.Fail(err!);

        return await _read.GetBalanceSheetAsync(userId, realmId, asOfDate ?? DateTime.UtcNow.Date, accountingMethod);
    }
}
