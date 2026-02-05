using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches official QuickBooks API "Update an invoice" request body.
    /// Ref: https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/invoice
    /// </summary>
    public class UpdateInvoiceRequest
    {
        [JsonPropertyName("TxnDate")]
        [MaxLength(10)]
        public string? TxnDate { get; set; }

        [JsonPropertyName("domain")]
        [MaxLength(10)]
        public string? Domain { get; set; } = "QBO";

        [JsonPropertyName("PrintStatus")]
        [MaxLength(50)]
        public string? PrintStatus { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonPropertyName("Line")]
        public List<UpdateInvoiceLineRequest>? Line { get; set; }

        [JsonPropertyName("DueDate")]
        [MaxLength(10)]
        public string? DueDate { get; set; }

        [JsonPropertyName("ApplyTaxAfterDiscount")]
        public bool? ApplyTaxAfterDiscount { get; set; }

        [JsonPropertyName("DocNumber")]
        [MaxLength(50)]
        public string? DocNumber { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; } = false;

        [JsonPropertyName("CustomerMemo")]
        public InvoiceCustomerMemo? CustomerMemo { get; set; }

        [JsonPropertyName("ProjectRef")]
        public InvoiceRef? ProjectRef { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("CustomerRef")]
        public InvoiceRef? CustomerRef { get; set; }

        [JsonPropertyName("TxnTaxDetail")]
        public InvoiceTxnTaxDetail? TxnTaxDetail { get; set; }

        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("LinkedTxn")]
        public List<InvoiceLinkedTxn>? LinkedTxn { get; set; }

        [JsonPropertyName("ShipAddr")]
        public InvoicePhysicalAddress? ShipAddr { get; set; }

        [JsonPropertyName("EmailStatus")]
        [MaxLength(50)]
        public string? EmailStatus { get; set; }

        [JsonPropertyName("BillAddr")]
        public InvoicePhysicalAddress? BillAddr { get; set; }

        [JsonPropertyName("MetaData")]
        public InvoiceMetaDataRequest? MetaData { get; set; }

        [JsonPropertyName("CustomField")]
        public List<InvoiceCustomField>? CustomField { get; set; }

        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = null!;
    }

    public class UpdateInvoiceLineRequest
    {
        [JsonPropertyName("LineNum")]
        public int? LineNum { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("SalesItemLineDetail")]
        public UpdateInvoiceSalesItemLineDetail? SalesItemLineDetail { get; set; }

        [JsonPropertyName("Id")]
        [MaxLength(50)]
        public string? Id { get; set; }

        [JsonPropertyName("DetailType")]
        [MaxLength(50)]
        public string? DetailType { get; set; }

        [JsonPropertyName("SubTotalLineDetail")]
        public object? SubTotalLineDetail { get; set; }

        [JsonPropertyName("Description")]
        [MaxLength(4000)]
        public string? Description { get; set; }
    }

    public class UpdateInvoiceSalesItemLineDetail
    {
        [JsonPropertyName("TaxCodeRef")]
        public InvoiceRef? TaxCodeRef { get; set; }

        [JsonPropertyName("ItemRef")]
        public InvoiceRef? ItemRef { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }
    }

    public class InvoiceCustomerMemo
    {
        [JsonPropertyName("value")]
        [MaxLength(1000)]
        public string? Value { get; set; }
    }

    public class InvoiceTxnTaxDetail
    {
        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }
    }

    public class InvoiceLinkedTxn
    {
        [JsonPropertyName("TxnId")]
        [MaxLength(50)]
        public string? TxnId { get; set; }

        [JsonPropertyName("TxnType")]
        [MaxLength(50)]
        public string? TxnType { get; set; }
    }

    public class InvoicePhysicalAddress
    {
        [JsonPropertyName("City")]
        [MaxLength(255)]
        public string? City { get; set; }

        [JsonPropertyName("Line1")]
        [MaxLength(500)]
        public string? Line1 { get; set; }

        [JsonPropertyName("PostalCode")]
        [MaxLength(30)]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Lat")]
        [MaxLength(50)]
        public string? Lat { get; set; }

        [JsonPropertyName("Long")]
        [MaxLength(50)]
        public string? Long { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        [MaxLength(255)]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("Id")]
        [MaxLength(50)]
        public string? Id { get; set; }

        [JsonPropertyName("Line2")]
        [MaxLength(500)]
        public string? Line2 { get; set; }

        [JsonPropertyName("Line3")]
        [MaxLength(500)]
        public string? Line3 { get; set; }

        [JsonPropertyName("Country")]
        [MaxLength(255)]
        public string? Country { get; set; }
    }

    public class InvoiceMetaDataRequest
    {
        [JsonPropertyName("CreateTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }
    }

    public class InvoiceCustomField
    {
        [JsonPropertyName("DefinitionId")]
        [MaxLength(50)]
        public string? DefinitionId { get; set; }

        [JsonPropertyName("Type")]
        [MaxLength(50)]
        public string? Type { get; set; }

        [JsonPropertyName("Name")]
        [MaxLength(255)]
        public string? Name { get; set; }

        [JsonPropertyName("StringValue")]
        [MaxLength(500)]
        public string? StringValue { get; set; }
    }
}
