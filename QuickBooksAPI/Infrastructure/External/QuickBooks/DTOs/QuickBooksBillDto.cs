using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    /// <summary>
    /// Root response for QuickBooks Bill create/update/delete (mutation).
    /// </summary>
    public class QuickBooksBillMutationResponse
    {
        [JsonPropertyName("Bill")]
        public QuickBooksBillDto? Bill { get; set; }

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }
    }

    /// <summary>
    /// Root response for QuickBooks Bill query (sync/list).
    /// </summary>
    public class QuickBooksBillQueryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public BillQueryResponse QueryResponse { get; set; } = null!;

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }
    }

    public class BillQueryResponse
    {
        [JsonPropertyName("startPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }

        [JsonPropertyName("Bill")]
        public List<QuickBooksBillDto>? Bill { get; set; }
    }

    /// <summary>
    /// QuickBooks Bill entity (vendor bill) in query/sync response.
    /// </summary>
    public class QuickBooksBillDto
    {
        [JsonPropertyName("Id")]
        public string QBOId { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("APAccountRef")]
        public Reference? APAccountRef { get; set; }

        [JsonPropertyName("VendorRef")]
        public Reference? VendorRef { get; set; }

        [JsonPropertyName("TxnDate")]
        public string? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal TotalAmt { get; set; }

        [JsonPropertyName("Balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("DocNumber")]
        public string? DocNumber { get; set; }

        [JsonPropertyName("PrivateNote")]
        public string? PrivateNote { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public Reference? CurrencyRef { get; set; }

        [JsonPropertyName("SalesTermRef")]
        public Reference? SalesTermRef { get; set; }

        [JsonPropertyName("DepartmentRef")]
        public Reference? DepartmentRef { get; set; }

        [JsonPropertyName("Line")]
        public List<BillLineDto>? Line { get; set; }

        [JsonPropertyName("MetaData")]
        public BillMetaDataDto? MetaData { get; set; }
    }

    public class BillLineDto
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("DetailType")]
        public string? DetailType { get; set; }

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("ProjectRef")]
        public Reference? ProjectRef { get; set; }

        [JsonPropertyName("AccountBasedExpenseLineDetail")]
        public BillAccountBasedExpenseLineDetailDto? AccountBasedExpenseLineDetail { get; set; }

        [JsonPropertyName("ItemBasedExpenseLineDetail")]
        public BillItemBasedExpenseLineDetailDto? ItemBasedExpenseLineDetail { get; set; }
    }

    public class BillAccountBasedExpenseLineDetailDto
    {
        [JsonPropertyName("AccountRef")]
        public Reference? AccountRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public Reference? TaxCodeRef { get; set; }

        [JsonPropertyName("BillableStatus")]
        public string? BillableStatus { get; set; }

        [JsonPropertyName("CustomerRef")]
        public Reference? CustomerRef { get; set; }

        [JsonPropertyName("MarkupInfo")]
        public BillMarkupInfoDto? MarkupInfo { get; set; }
    }

    public class BillMarkupInfoDto
    {
        [JsonPropertyName("Percent")]
        public decimal? Percent { get; set; }
    }

    public class BillItemBasedExpenseLineDetailDto
    {
        [JsonPropertyName("ItemRef")]
        public Reference? ItemRef { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public Reference? TaxCodeRef { get; set; }

        [JsonPropertyName("BillableStatus")]
        public string? BillableStatus { get; set; }
    }

    public class BillMetaDataDto
    {
        [JsonPropertyName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }
    }
}
