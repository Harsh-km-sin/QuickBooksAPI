using Microsoft.Extensions.Logging;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services.Reports;

/// <summary>
/// Pulls P&amp;L and Balance Sheet history from QBO and stores it.
///
/// Differs from the entity sync services in two ways, both inherent to reports:
///  - No paging. A report is one document per call, not a pageable row set.
///  - No MetaData.LastUpdatedTime watermark. A report is a computed snapshot with no row-level
///    change marker, so "incremental" is impossible; freshness is decided by the skip check below.
/// </summary>
public sealed class ReportQboSyncService : IReportQboSyncService
{
    /// <summary>
    /// Entity types whose changes can move a financial statement. If none has changed since the
    /// last report pull, re-pulling would produce byte-identical data.
    /// </summary>
    private static readonly string[] FinancialEntityTypes =
    {
        QboEntityType.Invoice.ToString(),
        QboEntityType.Bills.ToString(),
        QboEntityType.Manual_Journals.ToString()
    };

    /// <summary>Backstop when the company has no usable CompanyStartDate.</summary>
    private const int MaxBackfillYears = 10;

    private readonly IAuthService _authService;
    private readonly IQuickBooksReportsService _quickBooksReportsService;
    private readonly IReportRepository _reportRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IQboCompanyMetadataService _companyMetadataService;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly ILogger<ReportQboSyncService> _logger;

    public ReportQboSyncService(
        IAuthService authService,
        IQuickBooksReportsService quickBooksReportsService,
        IReportRepository reportRepository,
        ICompanyRepository companyRepository,
        IQboCompanyMetadataService companyMetadataService,
        IQboSyncStateRepository qboSyncStateRepository,
        ILogger<ReportQboSyncService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksReportsService = quickBooksReportsService ?? throw new ArgumentNullException(nameof(quickBooksReportsService));
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _companyMetadataService = companyMetadataService ?? throw new ArgumentNullException(nameof(companyMetadataService));
        _qboSyncStateRepository = qboSyncStateRepository ?? throw new ArgumentNullException(nameof(qboSyncStateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId, bool force = false)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var company = await _companyRepository.GetByUserAndRealmAsync(userId, realmId);
            if (company == null)
                return ApiResponse<int>.Fail("Company not found. Please reconnect QuickBooks.", new[] { "No company record for this realm" });

            // Refresh before reading the values below. Companies connected before this feature
            // existed have no metadata at all, and a basis change made in QBO is only observable
            // here — without this, the invalidation check further down could never fire.
            company = await _companyMetadataService.EnsureMetadataAsync(userId, realmId, token.AccessToken, company);

            var accountingMethod = string.IsNullOrWhiteSpace(company.AccountingBasis)
                ? Auth.QuickBooksCompanyMetadataParser.DefaultAccountingBasis
                : company.AccountingBasis;
            var fiscalYearStartMonth = company.FiscalYearStartMonth is >= 1 and <= 12
                ? company.FiscalYearStartMonth.Value
                : Auth.QuickBooksCompanyMetadataParser.DefaultFiscalYearStartMonth;

            // A basis or fiscal-year change invalidates every stored period: the numbers were
            // computed on a different basis, and the chunk boundaries no longer line up (stale runs
            // would overlap the new ones). Wipe and re-pull rather than trying to reconcile.
            var invalidated = await ClearInvalidatedRunsAsync(userId, realmId, accountingMethod, fiscalYearStartMonth);
            var mustRepull = force || invalidated;

            if (!mustRepull && !await HasFinancialDataChangedAsync(userId, realmId))
            {
                _logger.LogInformation(
                    "Reports sync skipped for UserId={UserId} RealmId={RealmId}: no financial entity changed since the last pull.",
                    userId, realmId);
                return ApiResponse<int>.Ok(0, "Reports are already up to date.");
            }

            var rangeEnd = EndOfMonth(DateTime.UtcNow.Date);
            var rangeStart = DetermineBackfillStart(company.CompanyStartDate, rangeEnd);

            var totalPeriods = 0;
            foreach (var reportType in ReportTypes.All)
            {
                totalPeriods += await SyncReportTypeAsync(
                    token.AccessToken,
                    userId,
                    realmId,
                    reportType,
                    accountingMethod,
                    fiscalYearStartMonth,
                    rangeStart,
                    rangeEnd);
            }

            _logger.LogInformation(
                "Reports sync stored {PeriodCount} periods for UserId={UserId} RealmId={RealmId} Method={AccountingMethod}",
                totalPeriods, userId, realmId, accountingMethod);

            return ApiResponse<int>.Ok(totalPeriods, $"Successfully synced {totalPeriods} report periods.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reports sync failed for UserId={UserId} RealmId={RealmId}", userId, realmId);
            return ApiResponse<int>.Fail("Failed to sync reports from QuickBooks.", new[] { ex.Message });
        }
    }

    private async Task<int> SyncReportTypeAsync(
        string accessToken,
        int userId,
        string realmId,
        string reportType,
        string accountingMethod,
        int fiscalYearStartMonth,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var periods = 0;

        foreach (var (chunkStart, chunkEnd) in BuildFiscalYearChunks(rangeStart, rangeEnd, fiscalYearStartMonth))
        {
            // chunkStart is always a fiscal-year start. For the Balance Sheet that is not merely a
            // chunking convenience: pinning start_date to the fiscal-year start is what keeps the
            // retained-earnings / net-income split stable and independent of our chunk boundaries.
            var json = reportType == ReportTypes.BalanceSheet
                ? await _quickBooksReportsService.GetBalanceSheetAsync(
                    accessToken, realmId, chunkStart, chunkEnd, ReportGranularity.Month, accountingMethod)
                : await _quickBooksReportsService.GetProfitAndLossAsync(
                    accessToken, realmId, chunkStart, chunkEnd, ReportGranularity.Month, accountingMethod);

            var flattened = QuickBooksReportMapper.Flatten(
                json,
                userId,
                realmId,
                reportType,
                ReportGranularity.Month,
                accountingMethod,
                chunkStart,
                chunkEnd);

            using var conn = _reportRepository.CreateOpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                await _reportRepository.UpsertReportRunAsync(flattened, conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            periods++;
        }

        await SafeUpdateSyncStateAsync(userId, realmId, reportType);
        return periods;
    }

    /// <summary>
    /// Fiscal-year windows covering the range. Chunking is required because a single call cannot
    /// safely carry an unbounded number of monthly columns, and fiscal-year alignment satisfies the
    /// Balance Sheet's start_date requirement at the same time.
    /// </summary>
    internal static IEnumerable<(DateTime Start, DateTime End)> BuildFiscalYearChunks(
        DateTime rangeStart,
        DateTime rangeEnd,
        int fiscalYearStartMonth)
    {
        var chunkStart = FiscalYearStartFor(rangeStart, fiscalYearStartMonth);

        while (chunkStart <= rangeEnd)
        {
            var fiscalYearEnd = chunkStart.AddYears(1).AddDays(-1);
            var chunkEnd = fiscalYearEnd > rangeEnd ? rangeEnd : fiscalYearEnd;

            yield return (chunkStart, chunkEnd);

            chunkStart = chunkStart.AddYears(1);
        }
    }

    internal static DateTime FiscalYearStartFor(DateTime date, int fiscalYearStartMonth) =>
        date.Month >= fiscalYearStartMonth
            ? new DateTime(date.Year, fiscalYearStartMonth, 1)
            : new DateTime(date.Year - 1, fiscalYearStartMonth, 1);

    private static DateTime EndOfMonth(DateTime date) =>
        new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);

    private static DateTime DetermineBackfillStart(DateTime? companyStartDate, DateTime rangeEnd)
    {
        var floor = rangeEnd.AddYears(-MaxBackfillYears);

        if (companyStartDate is null)
            return floor;

        // Guard against implausible values rather than trusting the field blindly.
        return companyStartDate.Value < floor ? floor : companyStartDate.Value;
    }

    /// <summary>
    /// Compares the entity sync watermarks against the last report pull. Reports have no row-level
    /// change marker of their own, so the entity watermarks are the only available change signal —
    /// there is no CDC or webhook path in this codebase.
    /// </summary>
    private async Task<bool> HasFinancialDataChangedAsync(int userId, string realmId)
    {
        var lastReportSync = await _reportRepository.GetLastSyncedAtAsync(userId, realmId, ReportTypes.ProfitAndLoss);
        if (lastReportSync is null)
            return true; // Never synced.

        foreach (var entityType in FinancialEntityTypes)
        {
            var watermark = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, entityType);
            if (watermark is null)
                continue;

            var watermarkUtc = watermark.Value.Kind == DateTimeKind.Utc
                ? watermark.Value
                : DateTime.SpecifyKind(watermark.Value, DateTimeKind.Utc);

            if (watermarkUtc > lastReportSync.Value.UtcDateTime)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Drops stored runs that were pulled under a different accounting basis or a different
    /// fiscal-year alignment. Returns true when anything was removed.
    /// </summary>
    private async Task<bool> ClearInvalidatedRunsAsync(
        int userId,
        string realmId,
        string accountingMethod,
        int fiscalYearStartMonth)
    {
        var invalidated = false;

        foreach (var reportType in ReportTypes.All)
        {
            var runs = (await _reportRepository.GetSyncedRunsAsync(userId, realmId, reportType)).ToList();
            if (runs.Count == 0)
                continue;

            var basisChanged = runs.Any(r => !string.Equals(r.AccountingMethod, accountingMethod, StringComparison.OrdinalIgnoreCase));
            var fiscalYearChanged = runs.Any(r => r.PeriodStart.Month != fiscalYearStartMonth);

            if (!basisChanged && !fiscalYearChanged)
                continue;

            _logger.LogWarning(
                "Discarding stored {ReportType} runs for UserId={UserId} RealmId={RealmId}: " +
                "BasisChanged={BasisChanged}, FiscalYearChanged={FiscalYearChanged}. A full re-pull follows.",
                reportType, userId, realmId, basisChanged, fiscalYearChanged);

            await _reportRepository.DeleteAllRunsAsync(userId, realmId, reportType);
            invalidated = true;
        }

        return invalidated;
    }

    private async Task SafeUpdateSyncStateAsync(int userId, string realmId, string reportType)
    {
        if (userId <= 0)
            return;

        try
        {
            await _qboSyncStateRepository.UpdateStatusAsync(userId, realmId, reportType, QboSyncStatus.Completed.ToString());
        }
        catch (Exception ex)
        {
            // Sync-state bookkeeping must never fail a successful data pull.
            _logger.LogWarning(ex, "Failed to update QBO sync state for {ReportType}", reportType);
        }
    }
}
