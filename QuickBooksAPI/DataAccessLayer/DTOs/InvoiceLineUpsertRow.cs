namespace QuickBooksAPI.DataAccessLayer.DTOs
{
    /// <summary>
    /// Row for invoice line TVP (dbo.InvoiceLineUpsertType). Includes QBOInvoiceId + RealmId to link to header.
    /// </summary>
    public class InvoiceLineUpsertRow
    {
        public string QBOInvoiceId { get; set; } = null!;
        public string RealmId { get; set; } = null!;

        public string? QBLineId { get; set; }
        public int? LineNum { get; set; }
        public string? DetailType { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }

        public string? ItemRefId { get; set; }
        public string? ItemRefName { get; set; }
        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? TaxCodeRef { get; set; }

        public string? RawLineJson { get; set; }
    }
}
