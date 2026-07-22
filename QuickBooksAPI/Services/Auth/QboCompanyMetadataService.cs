using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services.Auth;

/// <inheritdoc />
public sealed class QboCompanyMetadataService : IQboCompanyMetadataService
{
    private readonly IQuickBooksAuthService _quickBooksAuthService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<QboCompanyMetadataService> _logger;

    public QboCompanyMetadataService(
        IQuickBooksAuthService quickBooksAuthService,
        ICompanyRepository companyRepository,
        ILogger<QboCompanyMetadataService> logger)
    {
        _quickBooksAuthService = quickBooksAuthService ?? throw new ArgumentNullException(nameof(quickBooksAuthService));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Company> EnsureMetadataAsync(int userId, string realmId, string accessToken, Company company)
    {
        if (company == null) throw new ArgumentNullException(nameof(company));

        var startDate = company.CompanyStartDate;
        var fiscalYearStartMonth = company.FiscalYearStartMonth;
        var accountingBasis = company.AccountingBasis;

        // Two endpoints, two independent try blocks: a failure fetching one must not discard the
        // other, and neither may fail the sync that called us.
        try
        {
            var companyInfoJson = await _quickBooksAuthService.GetCompanyInfoAsync(accessToken, realmId);
            var companyInfo = JsonSerializer.Deserialize<QuickBooksCompanyInfoResponse>(companyInfoJson);

            startDate = QuickBooksCompanyMetadataParser.ParseCompanyStartDate(companyInfo?.CompanyInfo?.CompanyStartDate)
                        ?? startDate;
            fiscalYearStartMonth = QuickBooksCompanyMetadataParser.ParseFiscalYearStartMonth(
                companyInfo?.CompanyInfo?.FiscalYearStartMonth);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to refresh QuickBooks CompanyInfo for UserId={UserId}, RealmId={RealmId}. Keeping stored values.",
                userId, realmId);
        }

        try
        {
            var preferencesJson = await _quickBooksAuthService.GetPreferencesAsync(accessToken, realmId);
            var preferences = JsonSerializer.Deserialize<QuickBooksPreferencesResponse>(preferencesJson);

            accountingBasis = QuickBooksCompanyMetadataParser.ParseAccountingBasis(
                preferences?.Preferences?.ReportPrefs?.ReportBasis);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to refresh QuickBooks Preferences for UserId={UserId}, RealmId={RealmId}. Keeping stored value.",
                userId, realmId);
        }

        var changed =
            !string.Equals(accountingBasis, company.AccountingBasis, StringComparison.OrdinalIgnoreCase) ||
            startDate != company.CompanyStartDate ||
            fiscalYearStartMonth != company.FiscalYearStartMonth;

        if (!changed)
            return company;

        _logger.LogInformation(
            "QBO company metadata updated for UserId={UserId} RealmId={RealmId}. " +
            "Basis {OldBasis}->{NewBasis}, FiscalYearStartMonth {OldMonth}->{NewMonth}, StartDate {OldStart}->{NewStart}",
            userId, realmId,
            company.AccountingBasis, accountingBasis,
            company.FiscalYearStartMonth, fiscalYearStartMonth,
            company.CompanyStartDate, startDate);

        try
        {
            await _companyRepository.UpdateCompanyMetadataAsync(
                userId, realmId, accountingBasis, startDate, fiscalYearStartMonth);
        }
        catch (Exception ex)
        {
            // The in-memory values are still correct for this run; persistence retries next sync.
            _logger.LogWarning(ex,
                "Failed to persist QBO company metadata for UserId={UserId}, RealmId={RealmId}.",
                userId, realmId);
        }

        company.AccountingBasis = accountingBasis;
        company.CompanyStartDate = startDate;
        company.FiscalYearStartMonth = fiscalYearStartMonth;
        return company;
    }
}
