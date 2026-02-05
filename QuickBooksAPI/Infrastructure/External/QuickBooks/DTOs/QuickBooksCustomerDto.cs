using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksCustomerQueryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public CustomerQueryResponse QueryResponse { get; set; } = null!;
    }

    public class CustomerQueryResponse
    {
        [JsonPropertyName("Customer")]
        public List<QuickBooksCustomerDto>? Customers { get; set; }

        [JsonPropertyName("startPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }
    }

    public class QuickBooksCustomerMutationResponse
    {
        [JsonPropertyName("Customer")]
        public QuickBooksCustomerDto Customer { get; set; } = null!;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }
    public class QuickBooksCustomerDto
    {
        [JsonPropertyName("Id")]
        public string QBOId { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = null!;
        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("MiddleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("FullyQualifiedName")]
        public string FullyQualifiedName { get; set; } = null!;

        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        [JsonPropertyName("Taxable")]
        public bool Taxable { get; set; }

        [JsonPropertyName("Job")]
        public bool Job { get; set; }

        [JsonPropertyName("BillWithParent")]
        public bool BillWithParent { get; set; }

        [JsonPropertyName("Balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("BalanceWithJobs")]
        public decimal BalanceWithJobs { get; set; }

        [JsonPropertyName("PreferredDeliveryMethod")]
        public string? PreferredDeliveryMethod { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public PrimaryEmailAddr? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public PrimaryPhone? PrimaryPhone { get; set; }

        [JsonPropertyName("BillAddr")]
        public PhysicalAddress? BillAddr { get; set; }

        [JsonPropertyName("ShipAddr")]
        public PhysicalAddress? ShipAddr { get; set; }

        [JsonPropertyName("MetaData")]
        public CustomerMetaData MetaData { get; set; } = null!;
    }

    public class PrimaryEmailAddr
    {
        [JsonPropertyName("Address")]
        public string Address { get; set; } = null!;
    }

    public class PrimaryPhone
    {
        [JsonPropertyName("FreeFormNumber")]
        public string FreeFormNumber { get; set; } = null!;
    }

    public class PhysicalAddress
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

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

    public class CustomerMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }
    }
}
