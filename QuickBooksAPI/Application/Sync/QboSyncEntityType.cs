namespace QuickBooksAPI.Application.Sync;

/// <summary>
/// String keys stored in QBO sync state (<c>EntityType</c> column). Values must match persisted names (legacy <c>QboEntityType</c> enum <c>ToString()</c>).
/// </summary>
public static class QboSyncEntityType
{
    public const string Products = "Products";
    public const string ChartOfAccounts = "Chart_Of_Accounts";
}
