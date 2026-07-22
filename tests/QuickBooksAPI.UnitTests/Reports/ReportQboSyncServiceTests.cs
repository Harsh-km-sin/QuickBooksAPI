using Microsoft.Extensions.Logging;
using Moq;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Services.Reports;
using QuickBooksService.Services;

namespace QuickBooksAPI.UnitTests.Reports;

public sealed class ReportQboSyncServiceTests
{
    private const int UserId = 1;
    private const string RealmId = "realm";

    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IQuickBooksReportsService> _qbo = new();
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IQboCompanyMetadataService> _metadata = new();
    private readonly Mock<IQboSyncStateRepository> _syncState = new();

    private ReportQboSyncService CreateSut() => new(
        _auth.Object,
        _qbo.Object,
        _repo.Object,
        _companies.Object,
        _metadata.Object,
        _syncState.Object,
        Mock.Of<ILogger<ReportQboSyncService>>());

    private void GivenConnectedCompany(string basis = "Accrual", int fiscalYearStartMonth = 1)
    {
        _auth.Setup(a => a.RefreshTokenIfExpiredAsync(UserId, RealmId))
            .ReturnsAsync(new QboAccessTokenSnapshot { AccessToken = "token" });

        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            AccountingBasis = basis,
            FiscalYearStartMonth = fiscalYearStartMonth,
            CompanyStartDate = new DateTime(2024, 1, 1)
        };

        _companies.Setup(c => c.GetByUserAndRealmAsync(UserId, RealmId)).ReturnsAsync(company);

        // Default: metadata is already current, so the refresh returns the company untouched.
        _metadata.Setup(m => m.EnsureMetadataAsync(UserId, RealmId, It.IsAny<string>(), It.IsAny<Company>()))
            .ReturnsAsync((int _, string _, string _, Company c) => c);

        _repo.Setup(r => r.GetSyncedRunsAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<QBOReportRun>());

        _repo.Setup(r => r.CreateOpenConnection()).Returns(new FakeDbConnection());
    }

    private void GivenQboReturnsEmptyReport()
    {
        const string emptyReport = """
        {"Header":{"ReportName":"ProfitAndLoss","Currency":"USD"},"Columns":{"Column":[]},"Rows":{"Row":[]}}
        """;

        _qbo.Setup(q => q.GetProfitAndLossAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(emptyReport);

        _qbo.Setup(q => q.GetBalanceSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(emptyReport);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_NoToken_ReturnsFailure()
    {
        _auth.Setup(a => a.RefreshTokenIfExpiredAsync(UserId, RealmId))
            .ReturnsAsync((QboAccessTokenSnapshot?)null);

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId);

        Assert.False(result.Success);
        _qbo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_NoCompanyRecord_ReturnsFailure()
    {
        _auth.Setup(a => a.RefreshTokenIfExpiredAsync(UserId, RealmId))
            .ReturnsAsync(new QboAccessTokenSnapshot { AccessToken = "token" });
        _companies.Setup(c => c.GetByUserAndRealmAsync(UserId, RealmId)).ReturnsAsync((Company?)null);

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId);

        Assert.False(result.Success);
        _qbo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_EntitiesUnchanged_SkipsPull()
    {
        GivenConnectedCompany();

        var lastReportSync = DateTimeOffset.UtcNow;
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, ReportTypes.ProfitAndLoss))
            .ReturnsAsync(lastReportSync);

        // Every entity watermark predates the last report pull, so nothing financial has moved.
        _syncState.Setup(s => s.GetLastUpdatedAfterAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(lastReportSync.UtcDateTime.AddDays(-1));

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: false);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data);
        _qbo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_EntityChangedSinceLastPull_Pulls()
    {
        GivenConnectedCompany();
        GivenQboReturnsEmptyReport();

        var lastReportSync = DateTimeOffset.UtcNow.AddDays(-2);
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, ReportTypes.ProfitAndLoss))
            .ReturnsAsync(lastReportSync);
        _syncState.Setup(s => s.GetLastUpdatedAfterAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(DateTime.UtcNow);

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: false);

        Assert.True(result.Success);
        Assert.True(result.Data > 0);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_NeverSyncedBefore_Pulls()
    {
        GivenConnectedCompany();
        GivenQboReturnsEmptyReport();

        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync((DateTimeOffset?)null);

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: false);

        Assert.True(result.Success);
        Assert.True(result.Data > 0);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_Force_PullsEvenWhenUnchanged()
    {
        GivenConnectedCompany();
        GivenQboReturnsEmptyReport();

        var lastReportSync = DateTimeOffset.UtcNow;
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(lastReportSync);
        _syncState.Setup(s => s.GetLastUpdatedAfterAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(lastReportSync.UtcDateTime.AddDays(-1));

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: true);

        Assert.True(result.Success);
        Assert.True(result.Data > 0);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_RefreshesCompanyMetadataBeforeReadingIt()
    {
        GivenConnectedCompany();
        GivenQboReturnsEmptyReport();
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync((DateTimeOffset?)null);

        await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: true);

        _metadata.Verify(
            m => m.EnsureMetadataAsync(UserId, RealmId, "token", It.IsAny<Company>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_UsesMetadataRefreshedThisRunNotTheStoredValues()
    {
        // A company connected before this feature existed has no metadata stored. The refresh
        // supplies it mid-run, and the pull must use those fresh values.
        GivenConnectedCompany();
        GivenQboReturnsEmptyReport();
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync((DateTimeOffset?)null);

        _companies.Setup(c => c.GetByUserAndRealmAsync(UserId, RealmId))
            .ReturnsAsync(new Company { UserId = UserId, QboRealmId = RealmId });   // all metadata null

        _metadata.Setup(m => m.EnsureMetadataAsync(UserId, RealmId, It.IsAny<string>(), It.IsAny<Company>()))
            .ReturnsAsync((int _, string _, string _, Company c) =>
            {
                c.AccountingBasis = "Cash";
                c.FiscalYearStartMonth = 7;
                c.CompanyStartDate = new DateTime(2023, 7, 1);
                return c;
            });

        await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: true);

        _qbo.Verify(q => q.GetProfitAndLossAsync(
                It.IsAny<string>(),
                RealmId,
                It.Is<DateTime>(d => d.Month == 7 && d.Day == 1),
                It.IsAny<DateTime>(),
                ReportGranularity.Month,
                "Cash"),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_AccountingBasisChanged_DiscardsStoredRunsAndRepulls()
    {
        GivenConnectedCompany(basis: "Cash");
        GivenQboReturnsEmptyReport();

        // Stored runs were pulled on Accrual; the company is now on Cash, so they are unusable.
        _repo.Setup(r => r.GetSyncedRunsAsync(UserId, RealmId, ReportTypes.ProfitAndLoss))
            .ReturnsAsync(new[]
            {
                new QBOReportRun { AccountingMethod = "Accrual", PeriodStart = new DateTime(2024, 1, 1) }
            });

        // Skip check would otherwise short-circuit; invalidation must win.
        var lastReportSync = DateTimeOffset.UtcNow;
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(lastReportSync);
        _syncState.Setup(s => s.GetLastUpdatedAfterAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync(lastReportSync.UtcDateTime.AddDays(-1));

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: false);

        Assert.True(result.Success);
        _repo.Verify(r => r.DeleteAllRunsAsync(UserId, RealmId, ReportTypes.ProfitAndLoss), Times.Once);
        Assert.True(result.Data > 0);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_FiscalYearStartChanged_DiscardsStoredRuns()
    {
        GivenConnectedCompany(fiscalYearStartMonth: 4);
        GivenQboReturnsEmptyReport();

        // Stored runs start in January; the company's fiscal year now starts in April, so the
        // chunk boundaries no longer line up and stale runs would overlap the new ones.
        _repo.Setup(r => r.GetSyncedRunsAsync(UserId, RealmId, ReportTypes.BalanceSheet))
            .ReturnsAsync(new[]
            {
                new QBOReportRun { AccountingMethod = "Accrual", PeriodStart = new DateTime(2024, 1, 1) }
            });

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: false);

        Assert.True(result.Success);
        _repo.Verify(r => r.DeleteAllRunsAsync(UserId, RealmId, ReportTypes.BalanceSheet), Times.Once);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_BalanceSheetIsPulledWithFiscalYearStartAsStartDate()
    {
        GivenConnectedCompany(fiscalYearStartMonth: 4);
        GivenQboReturnsEmptyReport();
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync((DateTimeOffset?)null);

        await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: true);

        // Pinning start_date to the fiscal-year start is what keeps the retained-earnings split
        // stable and independent of our chunk boundaries.
        _qbo.Verify(q => q.GetBalanceSheetAsync(
                It.IsAny<string>(),
                RealmId,
                It.Is<DateTime>(d => d.Month == 4 && d.Day == 1),
                It.IsAny<DateTime>(),
                ReportGranularity.Month,
                "Accrual"),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncFromQuickBooksAsync_QboFailure_ReturnsFailureRatherThanThrowing()
    {
        GivenConnectedCompany();
        _repo.Setup(r => r.GetLastSyncedAtAsync(UserId, RealmId, It.IsAny<string>()))
            .ReturnsAsync((DateTimeOffset?)null);
        _qbo.Setup(q => q.GetProfitAndLossAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("429 ThrottleExceeded"));

        var result = await CreateSut().SyncFromQuickBooksAsync(UserId, RealmId, force: true);

        Assert.False(result.Success);
        Assert.Contains(result.Errors!, e => e.Contains("429"));
    }

    // ----------------------------------------------------------- chunking

    [Fact]
    public void BuildFiscalYearChunks_CalendarYear_ProducesOneChunkPerYear()
    {
        var chunks = ReportQboSyncService.BuildFiscalYearChunks(
            new DateTime(2022, 3, 15), new DateTime(2024, 6, 30), fiscalYearStartMonth: 1).ToList();

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new DateTime(2022, 1, 1), chunks[0].Start);
        Assert.Equal(new DateTime(2022, 12, 31), chunks[0].End);
        Assert.Equal(new DateTime(2024, 1, 1), chunks[2].Start);
        // Final chunk is clamped to the requested end rather than running into the future.
        Assert.Equal(new DateTime(2024, 6, 30), chunks[2].End);
    }

    [Fact]
    public void BuildFiscalYearChunks_NonCalendarFiscalYear_StartsEveryChunkOnTheFiscalMonth()
    {
        var chunks = ReportQboSyncService.BuildFiscalYearChunks(
            new DateTime(2023, 6, 1), new DateTime(2025, 1, 31), fiscalYearStartMonth: 4).ToList();

        Assert.All(chunks, c => Assert.Equal(4, c.Start.Month));
        Assert.All(chunks, c => Assert.Equal(1, c.Start.Day));
        Assert.Equal(new DateTime(2023, 4, 1), chunks[0].Start);
    }

    [Fact]
    public void BuildFiscalYearChunks_ChunksNeverOverlap()
    {
        var chunks = ReportQboSyncService.BuildFiscalYearChunks(
            new DateTime(2020, 1, 1), new DateTime(2025, 12, 31), fiscalYearStartMonth: 7).ToList();

        for (var i = 1; i < chunks.Count; i++)
            Assert.True(chunks[i].Start > chunks[i - 1].End, $"Chunk {i} overlaps chunk {i - 1}.");
    }

    [Theory]
    [InlineData(2024, 3, 15, 1, 2024, 1)]   // calendar year
    [InlineData(2024, 3, 15, 4, 2023, 4)]   // before the fiscal start month -> previous year
    [InlineData(2024, 5, 15, 4, 2024, 4)]   // on/after it -> current year
    public void FiscalYearStartFor_ResolvesTheContainingFiscalYear(
        int year, int month, int day, int fiscalStartMonth, int expectedYear, int expectedMonth)
    {
        var result = ReportQboSyncService.FiscalYearStartFor(new DateTime(year, month, day), fiscalStartMonth);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, 1), result);
    }
}
