using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksCompanyInfoResponse
    {
        [JsonPropertyName("CompanyInfo")]
        public QuickBooksCompanyInfo CompanyInfo { get; set; } = null!;
    }

    public class QuickBooksCompanyInfo
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("LegalName")]
        public string? LegalName { get; set; }

        /// <summary>Date the company started in QBO. Lower bound for report history backfill.</summary>
        [JsonPropertyName("CompanyStartDate")]
        public string? CompanyStartDate { get; set; }

        /// <summary>QBO returns a month *name* here (e.g. "January"), not a number. Parse via QuickBooksCompanyMetadataParser.</summary>
        [JsonPropertyName("FiscalYearStartMonth")]
        public string? FiscalYearStartMonth { get; set; }
    }

    public class QuickBooksPreferencesResponse
    {
        [JsonPropertyName("Preferences")]
        public QuickBooksPreferences? Preferences { get; set; }
    }

    public class QuickBooksPreferences
    {
        [JsonPropertyName("ReportPrefs")]
        public QuickBooksReportPrefs? ReportPrefs { get; set; }
    }

    public class QuickBooksReportPrefs
    {
        /// <summary>"Cash", "Accrual", or "Both".</summary>
        [JsonPropertyName("ReportBasis")]
        public string? ReportBasis { get; set; }
    }
}

