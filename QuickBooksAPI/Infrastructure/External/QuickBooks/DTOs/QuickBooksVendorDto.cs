using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksVendorQueryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public VendorQueryResponse QueryResponse { get; set; } = null!;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    public class VendorQueryResponse
    {
        [JsonPropertyName("Vendor")]
        public List<QuickBooksVendorQueryDto>? Vendor { get; set; }

        [JsonPropertyName("startPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }
    }

    public class QuickBooksVendorQueryDto
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = null!;

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("Balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("Vendor1099")]
        public bool Vendor1099 { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public CurrencyRef? CurrencyRef { get; set; }

        [JsonPropertyName("MetaData")]
        public VendorMetaData? MetaData { get; set; }

        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        [JsonPropertyName("V4IDPseudonym")]
        public string? V4IDPseudonym { get; set; }

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("AcctNum")]
        public string? AcctNum { get; set; }

        [JsonPropertyName("BillAddr")]
        public VendorBillAddrQueryDto? BillAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public VendorPhoneDto? PrimaryPhone { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public VendorEmailDto? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("WebAddr")]
        public VendorWebAddrDto? WebAddr { get; set; }
    }

    public class VendorBillAddrQueryDto
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("Long")]
        public string? Long { get; set; }
    }

    public class VendorMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }
    }

    public class QuickBooksVendorDto
    {
        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("Suffix")]
        public string? Suffix { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("TaxIdentifier")]
        public string? TaxIdentifier { get; set; }

        [JsonPropertyName("AcctNum")]
        public string? AcctNum { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public VendorEmailDto? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public VendorPhoneDto? PrimaryPhone { get; set; }

        [JsonPropertyName("Mobile")]
        public VendorPhoneDto? Mobile { get; set; }

        [JsonPropertyName("WebAddr")]
        public VendorWebAddrDto? WebAddr { get; set; }

        [JsonPropertyName("BillAddr")]
        public VendorBillAddrDto? BillAddr { get; set; }
    }

    public class QuickBooksVendorMutationResponse
    {
        [JsonPropertyName("Vendor")]
        public QuickBooksVendorQueryDto Vendor { get; set; } = null!;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    public class VendorBillAddrDto
    {
        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("Line2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("Line3")]
        public string? Line3 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }
    }

    public class VendorEmailDto
    {
        [JsonPropertyName("Address")]
        public string Address { get; set; } = null!;
    }

    public class VendorPhoneDto
    {
        [JsonPropertyName("FreeFormNumber")]
        public string FreeFormNumber { get; set; } = null!;
    }

    public class VendorWebAddrDto
    {
        [JsonPropertyName("URI")]
        public string URI { get; set; } = null!;
    }
}
