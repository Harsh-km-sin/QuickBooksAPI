namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QBOJournalEntryLine
    {
        public long JournalEntryLineId { get; set; } // DB PK
        public long JournalEntryId { get; set; }     // FK

        public string QBLineId { get; set; }
        public int? LineNum { get; set; }

        public string DetailType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string PostingType { get; set; }

        public string AccountRefId { get; set; }
        public string AccountRefName { get; set; }

        public string EntityType { get; set; }
        public string EntityRefId { get; set; }
        public string EntityRefName { get; set; }

        public string ProjectRefId { get; set; }

        public string RawLineJson { get; set; }
    }
}
