namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Invoice header returned by list/read APIs; maps from <see cref="DataAccessLayer.Models.QBOInvoiceHeader"/>.
/// </summary>
public sealed class InvoiceListItemDto
{
    public long InvoiceId { get; set; }
    public string QBOInvoiceId { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool Sparse { get; set; }
    public DateTime TxnDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string CustomerRefId { get; set; } = string.Empty;
    public string CustomerRefName { get; set; } = string.Empty;
    public decimal TotalAmt { get; set; }
    public decimal HomeTotalAmt { get; set; }
    public decimal Balance { get; set; }
    public decimal HomeBalance { get; set; }
    public string GlobalTaxCalculation { get; set; } = string.Empty;
    public string PrivateNote { get; set; } = string.Empty;
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset LastUpdatedTime { get; set; }
    public string RawJson { get; set; } = string.Empty;
}
