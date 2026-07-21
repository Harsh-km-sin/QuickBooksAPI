namespace QuickBooksAPI.Application.Reports;

/// <summary>
/// The QBO report types this product syncs. Values are used verbatim as the QBO endpoint segment,
/// as the <c>ReportType</c> discriminator in <c>dbo.QBOReportRun</c>, and as the
/// <c>QBO_Sync_State.EntityType</c> value — keep them identical in all three places.
/// </summary>
public static class ReportTypes
{
    public const string ProfitAndLoss = "ProfitAndLoss";
    public const string BalanceSheet = "BalanceSheet";

    public static readonly IReadOnlyList<string> All = new[] { ProfitAndLoss, BalanceSheet };
}

/// <summary>
/// Whether a report's stored amounts may be added across periods. Getting this wrong produces
/// plausible-looking, badly wrong numbers, so it is stored on every run and encoded in the
/// per-report-type read procedures rather than decided at call sites.
/// </summary>
public static class ReportValueSemantics
{
    /// <summary>Additive across columns. A quarter is Jan + Feb + Mar. (P&amp;L)</summary>
    public const string Flow = "Flow";

    /// <summary>Point-in-time. A quarter is the Mar 31 column alone; never summed. (Balance Sheet)</summary>
    public const string Stock = "Stock";

    public static string For(string reportType) => reportType switch
    {
        ReportTypes.ProfitAndLoss => Flow,
        ReportTypes.BalanceSheet => Stock,
        _ => throw new ArgumentOutOfRangeException(
            nameof(reportType),
            reportType,
            "Unknown report type. Add it to ReportValueSemantics.For before syncing it — defaulting " +
            "would risk summing a point-in-time report.")
    };
}

/// <summary>Granularity stored per run; maps to QBO's <c>summarize_column_by</c>.</summary>
public static class ReportGranularity
{
    public const string Month = "Month";
}
