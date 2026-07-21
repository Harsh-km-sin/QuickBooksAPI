using Microsoft.Extensions.Logging;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.Services.Reports;

/// <summary>
/// Serves reports from stored data. No QBO call happens on any read path — that is the whole point
/// of syncing at monthly grain, and it is what makes arbitrary month-aligned filtering fast.
///
/// All arithmetic lives in SQL (dbo.GetProfitAndLossTree / dbo.GetBalanceSheetTree). This service
/// only re-nests the returned flat rows into a tree.
/// </summary>
public sealed class ReportReadService : IReportReadService
{
    private readonly IReportRepository _reportRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ReportReadService> _logger;

    public ReportReadService(
        IReportRepository reportRepository,
        ICompanyRepository companyRepository,
        ILogger<ReportReadService> logger)
    {
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<ReportTreeDto>> GetProfitAndLossAsync(
        int userId, string realmId, DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            return ApiResponse<ReportTreeDto>.Fail("End date must be on or after the start date.");

        try
        {
            var accountingMethod = await ResolveAccountingMethodAsync(userId, realmId);

            // Snap to whole months: stored columns are monthly, so a partial month would silently
            // drop that month's data rather than return a partial figure.
            var rangeStart = StartOfMonth(startDate);
            var rangeEnd = EndOfMonth(endDate);

            var lines = await _reportRepository.GetProfitAndLossAsync(
                userId, realmId, rangeStart, rangeEnd, accountingMethod);

            return ApiResponse<ReportTreeDto>.Ok(new ReportTreeDto
            {
                ReportType = ReportTypes.ProfitAndLoss,
                AccountingMethod = accountingMethod,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                IsPointInTime = false,
                Rows = BuildTree(lines)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Profit and Loss for UserId={UserId} RealmId={RealmId}", userId, realmId);
            return ApiResponse<ReportTreeDto>.Fail("Failed to load the Profit and Loss report.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<ReportTreeDto>> GetBalanceSheetAsync(int userId, string realmId, DateTime asOfDate)
    {
        try
        {
            var accountingMethod = await ResolveAccountingMethodAsync(userId, realmId);
            var rangeEnd = EndOfMonth(asOfDate);

            var lines = await _reportRepository.GetBalanceSheetAsync(
                userId, realmId, StartOfMonth(asOfDate), rangeEnd, accountingMethod);

            return ApiResponse<ReportTreeDto>.Ok(new ReportTreeDto
            {
                ReportType = ReportTypes.BalanceSheet,
                AccountingMethod = accountingMethod,
                RangeStart = rangeEnd,   // A balance sheet is a single instant, not a span.
                RangeEnd = rangeEnd,
                IsPointInTime = true,
                Rows = BuildTree(lines)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Balance Sheet for UserId={UserId} RealmId={RealmId}", userId, realmId);
            return ApiResponse<ReportTreeDto>.Fail("Failed to load the Balance Sheet report.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<ReportTreeDto>> GetProfitAndLossForFiscalYearAsync(
        int userId, string realmId, DateTime anyDateInYear)
    {
        var company = await _companyRepository.GetByUserAndRealmAsync(userId, realmId);
        var fiscalYearStartMonth = company?.FiscalYearStartMonth is >= 1 and <= 12
            ? company.FiscalYearStartMonth!.Value
            : QuickBooksCompanyMetadataParser.DefaultFiscalYearStartMonth;

        var fiscalYearStart = ReportQboSyncService.FiscalYearStartFor(anyDateInYear, fiscalYearStartMonth);
        var fiscalYearEnd = fiscalYearStart.AddYears(1).AddDays(-1);

        return await GetProfitAndLossAsync(userId, realmId, fiscalYearStart, fiscalYearEnd);
    }

    public async Task<ApiResponse<IEnumerable<ReportPeriodDto>>> GetSyncedPeriodsAsync(
        int userId, string realmId, string reportType)
    {
        if (!ReportTypes.All.Contains(reportType))
            return ApiResponse<IEnumerable<ReportPeriodDto>>.Fail(
                $"Unknown report type '{reportType}'. Expected one of: {string.Join(", ", ReportTypes.All)}.");

        try
        {
            var runs = await _reportRepository.GetSyncedRunsAsync(userId, realmId, reportType);

            var periods = runs.Select(r => new ReportPeriodDto
            {
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd,
                AccountingMethod = r.AccountingMethod,
                Granularity = r.Granularity,
                NoReportData = r.NoReportData,
                SyncedAtUtc = r.SyncedAtUtc
            });

            return ApiResponse<IEnumerable<ReportPeriodDto>>.Ok(periods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list synced report periods for UserId={UserId} RealmId={RealmId}", userId, realmId);
            return ApiResponse<IEnumerable<ReportPeriodDto>>.Fail("Failed to load synced report periods.", new[] { ex.Message });
        }
    }

    private async Task<string> ResolveAccountingMethodAsync(int userId, string realmId)
    {
        var company = await _companyRepository.GetByUserAndRealmAsync(userId, realmId);
        return string.IsNullOrWhiteSpace(company?.AccountingBasis)
            ? QuickBooksCompanyMetadataParser.DefaultAccountingBasis
            : company!.AccountingBasis!;
    }

    /// <summary>
    /// Re-nests flat rows via ParentRowPath. RowPath is used rather than RowNumber because a range
    /// spanning a fiscal-year boundary is assembled from more than one run, and RowNumber is only
    /// unique within a run.
    /// </summary>
    internal static List<ReportNodeDto> BuildTree(IEnumerable<ReportLineRow> lines)
    {
        var ordered = lines.OrderBy(l => l.RowNumber).ToList();
        var nodesByPath = new Dictionary<string, ReportNodeDto>(StringComparer.Ordinal);
        var roots = new List<ReportNodeDto>();

        foreach (var line in ordered)
        {
            nodesByPath[line.RowPath] = new ReportNodeDto
            {
                RowPath = line.RowPath,
                Label = line.Label,
                RowType = line.RowType,
                GroupName = line.GroupName,
                AccountQboId = line.AccountQboId,
                Depth = line.Depth,
                IsSummary = line.IsSummaryRow != 0,
                Amount = line.Amount
            };
        }

        foreach (var line in ordered)
        {
            var node = nodesByPath[line.RowPath];

            // A row whose parent is missing is promoted to a root rather than dropped — losing a
            // row silently would understate the report.
            if (!string.IsNullOrEmpty(line.ParentRowPath) &&
                nodesByPath.TryGetValue(line.ParentRowPath, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    private static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    private static DateTime EndOfMonth(DateTime date) => new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
}
