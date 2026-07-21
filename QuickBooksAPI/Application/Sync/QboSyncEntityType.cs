namespace QuickBooksAPI.Application.Sync;

/// <summary>
/// String keys stored in QBO sync state (<c>EntityType</c> column). Values must match persisted names (legacy <c>QboEntityType</c> enum <c>ToString()</c>).
/// </summary>
public static class QboSyncEntityType
{
    public const string Products = "Products";
    public const string ChartOfAccounts = "Chart_Of_Accounts";

    // Reports are snapshots rather than row sets, so these rows track run status only; the
    // LastUpdatedAfter watermark is meaningless for them. Values match Application.Reports.ReportTypes.
    public const string ProfitAndLoss = "ProfitAndLoss";
    public const string BalanceSheet = "BalanceSheet";
}
