using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches QuickBooks API "Create a bill" request body.
    /// Same structure as full update where applicable; create omits Id, SyncToken, MetaData.
    /// </summary>
    public class CreateBillRequest
    {
        [JsonPropertyName("Line")]
        [Required]
        [MinLength(1)]
        public List<CreateBillLineRequest> Line { get; set; } = null!;

        [JsonPropertyName("VendorRef")]
        [Required]
        public BillRef VendorRef { get; set; } = null!;

        [JsonPropertyName("TxnDate")]
        [MaxLength(10)]
        public string? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        [MaxLength(10)]
        public string? DueDate { get; set; }

        [JsonPropertyName("DocNumber")]
        [MaxLength(50)]
        public string? DocNumber { get; set; }

        [JsonPropertyName("APAccountRef")]
        public BillRef? APAccountRef { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public BillRef? CurrencyRef { get; set; }

        [JsonPropertyName("PrivateNote")]
        [MaxLength(4000)]
        public string? PrivateNote { get; set; }

        [JsonPropertyName("SalesTermRef")]
        public BillRef? SalesTermRef { get; set; }

        [JsonPropertyName("DepartmentRef")]
        public BillRef? DepartmentRef { get; set; }
    }

    public class CreateBillLineRequest
    {
        [JsonPropertyName("DetailType")]
        [Required]
        [MaxLength(50)]
        public string DetailType { get; set; } = "AccountBasedExpenseLineDetail";

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("Id")]
        [MaxLength(50)]
        public string? Id { get; set; }

        [JsonPropertyName("Description")]
        [MaxLength(4000)]
        public string? Description { get; set; }

        [JsonPropertyName("AccountBasedExpenseLineDetail")]
        public CreateBillAccountBasedExpenseLineDetail? AccountBasedExpenseLineDetail { get; set; }

        [JsonPropertyName("ItemBasedExpenseLineDetail")]
        public CreateBillItemBasedExpenseLineDetail? ItemBasedExpenseLineDetail { get; set; }

        [JsonPropertyName("ProjectRef")]
        public BillRef? ProjectRef { get; set; }
    }

    public class CreateBillAccountBasedExpenseLineDetail
    {
        [JsonPropertyName("AccountRef")]
        public BillRef? AccountRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public BillRef? TaxCodeRef { get; set; }

        [JsonPropertyName("BillableStatus")]
        [MaxLength(50)]
        public string? BillableStatus { get; set; }

        [JsonPropertyName("CustomerRef")]
        public BillRef? CustomerRef { get; set; }

        [JsonPropertyName("MarkupInfo")]
        public BillMarkupInfo? MarkupInfo { get; set; }
    }

    public class CreateBillItemBasedExpenseLineDetail
    {
        [JsonPropertyName("ItemRef")]
        public BillRef? ItemRef { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public BillRef? TaxCodeRef { get; set; }

        [JsonPropertyName("BillableStatus")]
        [MaxLength(50)]
        public string? BillableStatus { get; set; }
    }

    public class BillMarkupInfo
    {
        [JsonPropertyName("Percent")]
        public decimal? Percent { get; set; }
    }

    /// <summary>
    /// Reference for Bill requests: value (required for refs), name (optional).
    /// </summary>
    public class BillRef
    {
        [JsonPropertyName("value")]
        [MaxLength(50)]
        public string? Value { get; set; }

        [JsonPropertyName("name")]
        [MaxLength(500)]
        public string? Name { get; set; }
    }
}
