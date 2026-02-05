using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches official QuickBooks API "Create an invoice" request body.
    /// Ref: https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/invoice
    /// </summary>
    public class CreateInvoiceRequest
    {
        [JsonPropertyName("Line")]
        [Required]
        [MinLength(1)]
        public List<CreateInvoiceLineRequest> Line { get; set; } = null!;

        [JsonPropertyName("CustomerRef")]
        [Required]
        public InvoiceRef CustomerRef { get; set; } = null!;

        [JsonPropertyName("TxnDate")]
        [MaxLength(10)]
        public string? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        [MaxLength(10)]
        public string? DueDate { get; set; }
    }

    public class CreateInvoiceLineRequest
    {
        [JsonPropertyName("DetailType")]
        [Required]
        [MaxLength(50)]
        public string DetailType { get; set; } = "SalesItemLineDetail";

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("SalesItemLineDetail")]
        public CreateInvoiceSalesItemLineDetail? SalesItemLineDetail { get; set; }

        [JsonPropertyName("Description")]
        [MaxLength(4000)]
        public string? Description { get; set; }
    }

    public class CreateInvoiceSalesItemLineDetail
    {
        [JsonPropertyName("ItemRef")]
        public InvoiceRef? ItemRef { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public InvoiceRef? TaxCodeRef { get; set; }
    }

    /// <summary>
    /// Reference object: "value" (required for refs), "name" (optional). Used for CustomerRef, ItemRef, etc.
    /// </summary>
    public class InvoiceRef
    {
        [JsonPropertyName("value")]
        [MaxLength(50)]
        public string? Value { get; set; }

        [JsonPropertyName("name")]
        [MaxLength(500)]
        public string? Name { get; set; }
    }
}
