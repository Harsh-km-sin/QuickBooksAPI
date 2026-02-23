using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches official QuickBooks API "Update a vendor" request body.
    /// Ref: https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/vendor
    /// </summary>
    public class UpdateVendorRequest
    {
        [JsonPropertyName("PrimaryEmailAddr")]
        public VendorEmailAddr? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("Vendor1099")]
        public bool? Vendor1099 { get; set; }

        [JsonPropertyName("domain")]
        [MaxLength(10)]
        public string? Domain { get; set; } = "QBO";

        [JsonPropertyName("Title")]
        [MaxLength(50)]
        public string? Title { get; set; }

        [JsonPropertyName("Suffix")]
        [MaxLength(50)]
        public string? Suffix { get; set; }

        [JsonPropertyName("GivenName")]
        [MaxLength(100)]
        public string? GivenName { get; set; }

        [JsonPropertyName("MiddleName")]
        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [JsonPropertyName("DisplayName")]
        [MaxLength(500)]
        public string? DisplayName { get; set; }

        [JsonPropertyName("BillAddr")]
        public UpdateVendorBillAddr? BillAddr { get; set; }

        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("PrintOnCheckName")]
        [MaxLength(500)]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("FamilyName")]
        [MaxLength(100)]
        public string? FamilyName { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public VendorPhone? PrimaryPhone { get; set; }

        [JsonPropertyName("Mobile")]
        public VendorPhone? Mobile { get; set; }

        [JsonPropertyName("TaxIdentifier")]
        [MaxLength(50)]
        public string? TaxIdentifier { get; set; }

        [JsonPropertyName("AcctNum")]
        [MaxLength(100)]
        public string? AcctNum { get; set; }

        [JsonPropertyName("CompanyName")]
        [MaxLength(500)]
        public string? CompanyName { get; set; }

        [JsonPropertyName("WebAddr")]
        public VendorWebAddr? WebAddr { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; } = false;

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = null!;

        [JsonPropertyName("MetaData")]
        public VendorMetaData? MetaData { get; set; }
    }

    /// <summary>
    /// BillAddr for update; includes Id, Lat, Long per official API.
    /// </summary>
    public class UpdateVendorBillAddr
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

    public class VendorMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }
    }
}
