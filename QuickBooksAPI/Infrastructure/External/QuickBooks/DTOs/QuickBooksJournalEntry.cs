using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksJournalEntryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public JournalEntryQueryResponse QueryResponse { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    public class JournalEntryQueryResponse
    {
        [JsonPropertyName("JournalEntry")]
        public List<JournalEntry> JournalEntry { get; set; }

        [JsonPropertyName("startPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class JournalEntry
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        // Keep string for safety; parse later if needed
        [JsonPropertyName("TxnDate")]
        public string TxnDate { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("Adjustment")]
        public bool Adjustment { get; set; }

        [JsonPropertyName("DocNumber")]
        public string DocNumber { get; set; }

        [JsonPropertyName("PrivateNote")]
        public string PrivateNote { get; set; }

        [JsonPropertyName("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonPropertyName("HomeTotalAmt")]
        public decimal? HomeTotalAmt { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public AccountRef CurrencyRef { get; set; }

        [JsonPropertyName("MetaData")]
        public MetaData MetaData { get; set; }

        [JsonPropertyName("Line")]
        public List<Line> Line { get; set; }

        // QBO often sends {}
        [JsonPropertyName("TxnTaxDetail")]
        public TxnTaxDetail TxnTaxDetail { get; set; }
    }

    public class Line
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("DetailType")]
        public string DetailType { get; set; }

        [JsonPropertyName("JournalEntryLineDetail")]
        public JournalEntryLineDetail JournalEntryLineDetail { get; set; }
    }

    public class JournalEntryLineDetail
    {
        [JsonPropertyName("PostingType")]
        public string PostingType { get; set; }

        [JsonPropertyName("AccountRef")]
        public AccountRef AccountRef { get; set; }

        // Optional � appears for AP/AR related JEs
        [JsonPropertyName("Entity")]
        public Entity Entity { get; set; }
    }

    public class AccountRef
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; }

        [JsonPropertyName("Ref")]
        public AccountRef Ref { get; set; }
    }

    public class TxnTaxDetail
    {
        [JsonPropertyName("TxnTaxCodeRef")]
        public AccountRef TxnTaxCodeRef { get; set; }

        // Nullable because QBO may send {}
        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }
    }

    public class MetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTimeOffset? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTimeOffset? LastUpdatedTime { get; set; }
    }
}
