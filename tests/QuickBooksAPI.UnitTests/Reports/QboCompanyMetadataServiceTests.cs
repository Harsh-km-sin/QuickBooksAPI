using Microsoft.Extensions.Logging;
using Moq;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Services.Auth;
using QuickBooksService.Services;

namespace QuickBooksAPI.UnitTests.Reports;

public sealed class QboCompanyMetadataServiceTests
{
    private const int UserId = 1;
    private const string RealmId = "realm";
    private const string AccessToken = "token";

    private readonly Mock<IQuickBooksAuthService> _qboAuth = new();
    private readonly Mock<ICompanyRepository> _companies = new();

    private QboCompanyMetadataService CreateSut() => new(
        _qboAuth.Object,
        _companies.Object,
        Mock.Of<ILogger<QboCompanyMetadataService>>());

    private void GivenQboReturns(string? companyStartDate, string? fiscalYearStartMonth, string? reportBasis)
    {
        _qboAuth.Setup(a => a.GetCompanyInfoAsync(AccessToken, RealmId))
            .ReturnsAsync(
                "{\"CompanyInfo\":{\"Id\":\"1\",\"CompanyName\":\"Acme\"," +
                "\"CompanyStartDate\":" + Json(companyStartDate) + "," +
                "\"FiscalYearStartMonth\":" + Json(fiscalYearStartMonth) + "}}");

        _qboAuth.Setup(a => a.GetPreferencesAsync(AccessToken, RealmId))
            .ReturnsAsync(
                "{\"Preferences\":{\"ReportPrefs\":{\"ReportBasis\":" + Json(reportBasis) + "}}}");
    }

    private static string Json(string? value) => value is null ? "null" : $"\"{value}\"";

    [Fact]
    public async Task EnsureMetadataAsync_CompanyWithNoStoredMetadata_PopulatesAndPersistsIt()
    {
        // The case that motivated this: a company connected before the feature existed.
        GivenQboReturns("2021-04-01", "April", "Cash");
        var company = new Company { UserId = UserId, QboRealmId = RealmId };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        Assert.Equal("Cash", result.AccountingBasis);
        Assert.Equal(4, result.FiscalYearStartMonth);
        Assert.Equal(new DateTime(2021, 4, 1), result.CompanyStartDate);

        _companies.Verify(c => c.UpdateCompanyMetadataAsync(
            UserId, RealmId, "Cash", new DateTime(2021, 4, 1), 4), Times.Once);
    }

    [Fact]
    public async Task EnsureMetadataAsync_MetadataUnchanged_DoesNotWriteToTheDatabase()
    {
        GivenQboReturns("2021-04-01", "April", "Cash");
        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            AccountingBasis = "Cash",
            CompanyStartDate = new DateTime(2021, 4, 1),
            FiscalYearStartMonth = 4
        };

        await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        _companies.Verify(c => c.UpdateCompanyMetadataAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureMetadataAsync_BasisChangedInQbo_IsDetectedAndPersisted()
    {
        // This is what makes the "basis changed -> discard stored reports" path reachable at all.
        GivenQboReturns("2021-01-01", "January", "Cash");
        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            AccountingBasis = "Accrual",
            CompanyStartDate = new DateTime(2021, 1, 1),
            FiscalYearStartMonth = 1
        };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        Assert.Equal("Cash", result.AccountingBasis);
        _companies.Verify(c => c.UpdateCompanyMetadataAsync(
            UserId, RealmId, "Cash", It.IsAny<DateTime?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task EnsureMetadataAsync_PreferencesCallFails_KeepsStoredBasisAndStillAppliesCompanyInfo()
    {
        _qboAuth.Setup(a => a.GetCompanyInfoAsync(AccessToken, RealmId))
            .ReturnsAsync("""{"CompanyInfo":{"Id":"1","CompanyStartDate":"2020-02-01","FiscalYearStartMonth":"July"}}""");
        _qboAuth.Setup(a => a.GetPreferencesAsync(AccessToken, RealmId))
            .ThrowsAsync(new HttpRequestException("boom"));

        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            AccountingBasis = "Accrual"
        };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        // One endpoint failing must not discard what the other returned.
        Assert.Equal("Accrual", result.AccountingBasis);
        Assert.Equal(7, result.FiscalYearStartMonth);
        Assert.Equal(new DateTime(2020, 2, 1), result.CompanyStartDate);
    }

    [Fact]
    public async Task EnsureMetadataAsync_CompanyInfoCallFails_KeepsStoredValuesAndStillAppliesPreferences()
    {
        _qboAuth.Setup(a => a.GetCompanyInfoAsync(AccessToken, RealmId))
            .ThrowsAsync(new HttpRequestException("boom"));
        _qboAuth.Setup(a => a.GetPreferencesAsync(AccessToken, RealmId))
            .ReturnsAsync("""{"Preferences":{"ReportPrefs":{"ReportBasis":"Cash"}}}""");

        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            CompanyStartDate = new DateTime(2019, 5, 1),
            FiscalYearStartMonth = 3
        };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        Assert.Equal("Cash", result.AccountingBasis);
        Assert.Equal(new DateTime(2019, 5, 1), result.CompanyStartDate);
        Assert.Equal(3, result.FiscalYearStartMonth);
    }

    [Fact]
    public async Task EnsureMetadataAsync_PersistenceFails_DoesNotThrowSoSyncStillRuns()
    {
        GivenQboReturns("2021-04-01", "April", "Cash");
        _companies.Setup(c => c.UpdateCompanyMetadataAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var company = new Company { UserId = UserId, QboRealmId = RealmId };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        // In-memory values remain correct for this run; persistence retries on the next sync.
        Assert.Equal("Cash", result.AccountingBasis);
        Assert.Equal(4, result.FiscalYearStartMonth);
    }

    [Fact]
    public async Task EnsureMetadataAsync_BothCallsFail_ReturnsCompanyUnchanged()
    {
        _qboAuth.Setup(a => a.GetCompanyInfoAsync(AccessToken, RealmId)).ThrowsAsync(new HttpRequestException("a"));
        _qboAuth.Setup(a => a.GetPreferencesAsync(AccessToken, RealmId)).ThrowsAsync(new HttpRequestException("b"));

        var company = new Company
        {
            UserId = UserId,
            QboRealmId = RealmId,
            AccountingBasis = "Accrual",
            CompanyStartDate = new DateTime(2022, 1, 1),
            FiscalYearStartMonth = 1
        };

        var result = await CreateSut().EnsureMetadataAsync(UserId, RealmId, AccessToken, company);

        Assert.Equal("Accrual", result.AccountingBasis);
        Assert.Equal(new DateTime(2022, 1, 1), result.CompanyStartDate);
        Assert.Equal(1, result.FiscalYearStartMonth);
        _companies.Verify(c => c.UpdateCompanyMetadataAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int?>()),
            Times.Never);
    }
}
