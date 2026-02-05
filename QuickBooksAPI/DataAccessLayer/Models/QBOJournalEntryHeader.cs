namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QBOJournalEntryHeader
    {
        // DB PK (optional for inserts)
        public long JournalEntryId { get; set; }

        // QBO identity
        public string QBJournalEntryId { get; set; }
        public string QBRealmId { get; set; }
        public string SyncToken { get; set; }

        // QBO metadata
        public string Domain { get; set; }
        public bool? Sparse { get; set; }
        public bool? Adjustment { get; set; }

        // Transaction details
        public DateTime? TxnDate { get; set; }
        public string DocNumber { get; set; }
        public string PrivateNote { get; set; }

        // Currency & totals
        public string CurrencyCode { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? HomeTotalAmount { get; set; }

        // Audit / sync control
        public DateTimeOffset? CreateTime { get; set; }
        public DateTimeOffset? LastUpdatedTime { get; set; }

        // Raw QBO payload (single JournalEntry object)
        public string RawJson { get; set; }
    }
}
