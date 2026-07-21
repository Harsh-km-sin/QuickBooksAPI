using System.Globalization;

namespace QuickBooksAPI.Services.Auth;

/// <summary>
/// Parses the QBO company-metadata fields that report sync depends on. All parsers are lenient:
/// this metadata is fetched best-effort during OAuth connect, so a malformed value must degrade to
/// a sane default rather than break the connection.
/// </summary>
internal static class QuickBooksCompanyMetadataParser
{
    /// <summary>QBO's report default, and our fallback when the company is configured as "Both".</summary>
    public const string DefaultAccountingBasis = "Accrual";

    /// <summary>Calendar-year default when QBO gives us nothing usable.</summary>
    public const int DefaultFiscalYearStartMonth = 1;

    /// <summary>
    /// Normalizes <c>ReportPrefs.ReportBasis</c> to a value the Reports API accepts.
    /// QBO also returns "Both", which is not a valid <c>accounting_method</c> — those companies fall back to Accrual.
    /// </summary>
    public static string ParseAccountingBasis(string? reportBasis)
    {
        if (string.IsNullOrWhiteSpace(reportBasis))
            return DefaultAccountingBasis;

        if (string.Equals(reportBasis, "Cash", StringComparison.OrdinalIgnoreCase))
            return "Cash";

        if (string.Equals(reportBasis, "Accrual", StringComparison.OrdinalIgnoreCase))
            return "Accrual";

        // "Both" (or anything unexpected) has no single basis to inherit.
        return DefaultAccountingBasis;
    }

    /// <summary>
    /// QBO reports the fiscal year start as a month *name* ("January"). Accepts a numeric value too,
    /// since the field is loosely typed across minor versions.
    /// </summary>
    public static int ParseFiscalYearStartMonth(string? fiscalYearStartMonth)
    {
        if (string.IsNullOrWhiteSpace(fiscalYearStartMonth))
            return DefaultFiscalYearStartMonth;

        var value = fiscalYearStartMonth.Trim();

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
            return numeric is >= 1 and <= 12 ? numeric : DefaultFiscalYearStartMonth;

        var monthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;
        for (var i = 0; i < 12; i++)
        {
            if (string.Equals(monthNames[i], value, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }

        var abbreviated = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;
        for (var i = 0; i < 12; i++)
        {
            if (string.Equals(abbreviated[i], value, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }

        return DefaultFiscalYearStartMonth;
    }

    /// <summary>
    /// Parses <c>CompanyStartDate</c> ("yyyy-MM-dd"). Null when absent or unparseable — report sync then
    /// walks backwards to discover the earliest period with data instead of trusting a bad date.
    /// </summary>
    public static DateTime? ParseCompanyStartDate(string? companyStartDate)
    {
        if (string.IsNullOrWhiteSpace(companyStartDate))
            return null;

        if (DateTime.TryParseExact(
                companyStartDate.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed.Date;
        }

        return DateTime.TryParse(
            companyStartDate,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var loose)
            ? loose.Date
            : null;
    }
}
