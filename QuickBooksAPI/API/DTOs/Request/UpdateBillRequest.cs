using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches QuickBooks API "Full update a bill" request body.
    /// </summary>
    public class UpdateBillRequest
    {
        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("DocNumber")]
        [MaxLength(50)]
        public string? DocNumber { get; set; }

        [JsonPropertyName("domain")]
        [MaxLength(10)]
        public string? Domain { get; set; } = "QBO";

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("APAccountRef")]
        public BillRef? APAccountRef { get; set; }

        [JsonPropertyName("VendorRef")]
        public BillRef? VendorRef { get; set; }

        [JsonPropertyName("TxnDate")]
        [MaxLength(10)]
        public string? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        [MaxLength(10)]
        public string? DueDate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public BillRef? CurrencyRef { get; set; }

        [JsonPropertyName("PrivateNote")]
        [MaxLength(4000)]
        public string? PrivateNote { get; set; }

        [JsonPropertyName("SalesTermRef")]
        public BillRef? SalesTermRef { get; set; }

        [JsonPropertyName("DepartmentRef")]
        public BillRef? DepartmentRef { get; set; }

        [JsonPropertyName("Line")]
        public List<UpdateBillLineRequest>? Line { get; set; }

        [JsonPropertyName("MetaData")]
        public BillMetaDataRequest? MetaData { get; set; }
    }

    public class UpdateBillLineRequest
    {
        [JsonPropertyName("Id")]
        [MaxLength(50)]
        public string? Id { get; set; }

        [JsonPropertyName("Description")]
        [MaxLength(4000)]
        public string? Description { get; set; }

        [JsonPropertyName("DetailType")]
        [MaxLength(50)]
        public string? DetailType { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("ProjectRef")]
        public BillRef? ProjectRef { get; set; }

        [JsonPropertyName("AccountBasedExpenseLineDetail")]
        public CreateBillAccountBasedExpenseLineDetail? AccountBasedExpenseLineDetail { get; set; }

        [JsonPropertyName("ItemBasedExpenseLineDetail")]
        public CreateBillItemBasedExpenseLineDetail? ItemBasedExpenseLineDetail { get; set; }
    }

    public class BillMetaDataRequest
    {
        [JsonPropertyName("CreateTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }
    }
}
