namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Bill header returned by list/read APIs; maps from <see cref="DataAccessLayer.Models.QBOBillHeader"/>.
/// </summary>
public sealed class BillListItemDto
{
    public long BillId { get; set; }
    public string QBOBillId { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public bool Sparse { get; set; }
    public string? APAccountRefValue { get; set; }
    public string? APAccountRefName { get; set; }
    public string? VendorRefValue { get; set; }
    public string? VendorRefName { get; set; }
    public DateTime? TxnDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmt { get; set; }
    public decimal Balance { get; set; }
    public bool IsDeleted { get; set; }
    public string? CurrencyRefValue { get; set; }
    public string? CurrencyRefName { get; set; }
    public string? SalesTermRefValue { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset LastUpdatedTime { get; set; }
    public string? RawJson { get; set; }
}
