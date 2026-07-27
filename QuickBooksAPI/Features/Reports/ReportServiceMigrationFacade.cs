using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.Features.Reports.Handlers;

namespace QuickBooksAPI.Features.Reports;

/// <summary>
/// Façade wiring <see cref="IReportService"/> to the feature handlers, matching the shape used by
/// the other entity features.
/// </summary>
public sealed class ReportServiceMigrationFacade : IReportService
{
    private readonly GetReportHandler _get;
    private readonly ListReportPeriodsHandler _periods;
    private readonly SyncReportsHandler _sync;

    public ReportServiceMigrationFacade(
        GetReportHandler get,
        ListReportPeriodsHandler periods,
        SyncReportsHandler sync)
    {
        _get = get;
        _periods = periods;
        _sync = sync;
    }

    public Task<ApiResponse<ReportTreeDto>> GetProfitAndLossAsync(
        DateTime? startDate, DateTime? endDate, bool useFiscalYear, AccountingMethod? accountingMethod = null) =>
        _get.HandleProfitAndLossAsync(startDate, endDate, useFiscalYear, accountingMethod);

    public Task<ApiResponse<ReportTreeDto>> GetBalanceSheetAsync(
        DateTime? asOfDate, AccountingMethod? accountingMethod = null) =>
        _get.HandleBalanceSheetAsync(asOfDate, accountingMethod);

    public Task<ApiResponse<IEnumerable<ReportPeriodDto>>> GetSyncedPeriodsAsync(string reportType) =>
        _periods.HandleAsync(reportType);

    public Task<ApiResponse<int>> SyncReportsAsync() => _sync.HandleAsync(force: true);

    public Task<ApiResponse<int>> SyncReportsForFullSyncAsync() => _sync.HandleAsync(force: false);
}
