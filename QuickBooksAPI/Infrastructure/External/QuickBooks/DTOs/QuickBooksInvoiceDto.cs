using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksInvoiceQueryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public InvoiceQueryResponse QueryResponse { get; set; } = null!;
    }

    public class QuickBooksInvoiceMutationResponse
    {
        [JsonPropertyName("Invoice")]
        public QuickBooksInvoiceDto? Invoice { get; set; }

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }
    }

    public class InvoiceQueryResponse
    {
        [JsonPropertyName("Invoice")]
        public List<QuickBooksInvoiceDto>? Invoice { get; set; }

        [JsonPropertyName("startPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }

        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }
    }

    public class QuickBooksInvoiceDto
    {
        [JsonPropertyName("Id")]
        public string QBOId { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = null!;

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("AllowIPNPayment")]
        public bool AllowIPNPayment { get; set; }

        [JsonPropertyName("AllowOnlinePayment")]
        public bool AllowOnlinePayment { get; set; }

        [JsonPropertyName("AllowOnlineCreditCardPayment")]
        public bool AllowOnlineCreditCardPayment { get; set; }

        [JsonPropertyName("AllowOnlineACHPayment")]
        public bool AllowOnlineACHPayment { get; set; }

        [JsonPropertyName("TxnDate")]
        public string TxnDate { get; set; } = null!;

        [JsonPropertyName("DueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public CurrencyRef? CurrencyRef { get; set; }

        [JsonPropertyName("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal TotalAmt { get; set; }

        [JsonPropertyName("HomeTotalAmt")]
        public decimal? HomeTotalAmt { get; set; }

        [JsonPropertyName("Balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("HomeBalance")]
        public decimal? HomeBalance { get; set; }

        [JsonPropertyName("GlobalTaxCalculation")]
        public string? GlobalTaxCalculation { get; set; }

        [JsonPropertyName("PrintStatus")]
        public string? PrintStatus { get; set; }

        [JsonPropertyName("EmailStatus")]
        public string? EmailStatus { get; set; }

        [JsonPropertyName("FreeFormAddress")]
        public bool FreeFormAddress { get; set; }

        [JsonPropertyName("CustomerRef")]
        public Reference? CustomerRef { get; set; }

        [JsonPropertyName("BillAddr")]
        public InvoiceAddress? BillAddr { get; set; }

        [JsonPropertyName("ShipAddr")]
        public InvoiceAddress? ShipAddr { get; set; }

        [JsonPropertyName("ShipFromAddr")]
        public InvoiceAddress? ShipFromAddr { get; set; }

        [JsonPropertyName("Line")]
        public List<InvoiceLine>? Line { get; set; }

        [JsonPropertyName("TxnTaxDetail")]
        public TxnTaxDetail? TxnTaxDetail { get; set; }

        [JsonPropertyName("LinkedTxn")]
        public List<LinkedTxn>? LinkedTxn { get; set; }

        [JsonPropertyName("CustomField")]
        public List<CustomField>? CustomField { get; set; }

        [JsonPropertyName("MetaData")]
        public InvoiceMetaData? MetaData { get; set; }
    }

    public class InvoiceAddress
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("Line2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("Line3")]
        public string? Line3 { get; set; }

        [JsonPropertyName("Line4")]
        public string? Line4 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("Lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("Long")]
        public string? Long { get; set; }
    }

    public class InvoiceLine
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("LineNum")]
        public int? LineNum { get; set; }

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("DetailType")]
        public string DetailType { get; set; } = null!;

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("SalesItemLineDetail")]
        public SalesItemLineDetail? SalesItemLineDetail { get; set; }

        [JsonPropertyName("SubTotalLineDetail")]
        public SubTotalLineDetail? SubTotalLineDetail { get; set; }

        [JsonPropertyName("GroupLineDetail")]
        public GroupLineDetail? GroupLineDetail { get; set; }

        [JsonPropertyName("DiscountLineDetail")]
        public DiscountLineDetail? DiscountLineDetail { get; set; }

        [JsonPropertyName("CustomExtensions")]
        public List<object>? CustomExtensions { get; set; }
    }

    public class SalesItemLineDetail
    {
        [JsonPropertyName("ItemRef")]
        public Reference? ItemRef { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("ItemAccountRef")]
        public Reference? ItemAccountRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public Reference? TaxCodeRef { get; set; }

        [JsonPropertyName("MarkupInfo")]
        public MarkupInfo? MarkupInfo { get; set; }
    }

    public class SubTotalLineDetail
    {
        // This is typically an empty object in QuickBooks
    }

    public class GroupLineDetail
    {
        [JsonPropertyName("GroupItemRef")]
        public Reference? GroupItemRef { get; set; }

        [JsonPropertyName("Quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("Line")]
        public List<InvoiceLine>? Line { get; set; }
    }

    public class DiscountLineDetail
    {
        [JsonPropertyName("DiscountPercent")]
        public decimal? DiscountPercent { get; set; }

        [JsonPropertyName("DiscountAccountRef")]
        public Reference? DiscountAccountRef { get; set; }

        [JsonPropertyName("ClassRef")]
        public Reference? ClassRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public Reference? TaxCodeRef { get; set; }
    }

    public class MarkupInfo
    {
        [JsonPropertyName("PriceLevelRef")]
        public Reference? PriceLevelRef { get; set; }

        [JsonPropertyName("Percent")]
        public decimal? Percent { get; set; }

        [JsonPropertyName("MarkUpIncomeAccountRef")]
        public Reference? MarkUpIncomeAccountRef { get; set; }
    }

    public class TaxLine
    {
        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("DetailType")]
        public string? DetailType { get; set; }

        [JsonPropertyName("TaxLineDetail")]
        public TaxLineDetail? TaxLineDetail { get; set; }
    }

    public class TaxLineDetail
    {
        [JsonPropertyName("TaxRateRef")]
        public Reference? TaxRateRef { get; set; }

        [JsonPropertyName("PercentBased")]
        public bool? PercentBased { get; set; }

        [JsonPropertyName("TaxPercent")]
        public decimal? TaxPercent { get; set; }

        [JsonPropertyName("NetAmountTaxable")]
        public decimal? NetAmountTaxable { get; set; }
    }

    public class LinkedTxn
    {
        [JsonPropertyName("TxnId")]
        public string TxnId { get; set; } = null!;

        [JsonPropertyName("TxnType")]
        public string TxnType { get; set; } = null!;

        [JsonPropertyName("TxnLineId")]
        public string? TxnLineId { get; set; }
    }

    public class CustomField
    {
        [JsonPropertyName("DefinitionId")]
        public string? DefinitionId { get; set; }

        [JsonPropertyName("StringValue")]
        public string? StringValue { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }
    }

    public class InvoiceMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }

        [JsonPropertyName("LastModifiedByRef")]
        public Reference? LastModifiedByRef { get; set; }
    }

    public class Reference
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
