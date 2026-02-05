namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QBOInvoiceLine
    {
        // Local DB PK
        public long InvoiceLineId { get; set; }

        // FK to Invoice Header
        public long InvoiceId { get; set; }

        // QBO line identity
        public string QBLineId { get; set; }
        public int? LineNum { get; set; }

        public string DetailType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        // Sales item detail
        public string ItemRefId { get; set; }
        public string ItemRefName { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Qty { get; set; }

        public string ItemAccountRefId { get; set; }
        public string ItemAccountRefName { get; set; }

        public string TaxCodeRef { get; set; }

        // Raw line JSON
        public string RawLineJson { get; set; }
    }

}
