namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QBOInvoiceHeader
    {
        // Local DB PK
        public long InvoiceId { get; set; }

        // QBO identity
        public string QBOInvoiceId { get; set; } = null!;
        public string RealmId { get; set; } = null!;
        public string SyncToken { get; set; } = null!;

        // Metadata
        public string Domain { get; set; }
        public bool Sparse { get; set; }

        public DateTime TxnDate { get; set; }
        public DateTime DueDate { get; set; }

        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }

        public string CustomerRefId { get; set; }
        public string CustomerRefName { get; set; }

        public decimal TotalAmt { get; set; }
        public decimal HomeTotalAmt { get; set; }
        public decimal Balance { get; set; }
        public decimal HomeBalance { get; set; }

        public string GlobalTaxCalculation { get; set; }

        public string PrivateNote { get; set; }

        // Audit
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }

        // Raw QBO JSON
        public string RawJson { get; set; }
    }

}
