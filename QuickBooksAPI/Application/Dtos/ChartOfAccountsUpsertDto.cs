namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Chart-of-accounts row sent to persistence for upsert (sync path). Maps to TVP columns at the repository boundary.
/// </summary>
public sealed class ChartOfAccountsUpsertDto
{
    public string QboId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool SubAccount { get; set; }
    public string FullyQualifiedName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountSubType { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal CurrentBalanceWithSubAccounts { get; set; }
    public string CurrencyRefValue { get; set; } = string.Empty;
    public string CurrencyRefName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool Sparse { get; set; }
    public string SyncToken { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public int UserId { get; set; }
    public string RealmId { get; set; } = string.Empty;
}
