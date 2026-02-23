using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches official QuickBooks API "Create a vendor" request body.
    /// Ref: https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/vendor
    /// </summary>
    public class CreateVendorRequest
    {
        [JsonPropertyName("PrimaryEmailAddr")]
        public VendorEmailAddr? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("WebAddr")]
        public VendorWebAddr? WebAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public VendorPhone? PrimaryPhone { get; set; }

        [JsonPropertyName("DisplayName")]
        [Required]
        [MaxLength(500)]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("Suffix")]
        [MaxLength(50)]
        public string? Suffix { get; set; }

        [JsonPropertyName("Title")]
        [MaxLength(50)]
        public string? Title { get; set; }

        [JsonPropertyName("Mobile")]
        public VendorPhone? Mobile { get; set; }

        [JsonPropertyName("MiddleName")]
        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [JsonPropertyName("FamilyName")]
        [MaxLength(100)]
        public string? FamilyName { get; set; }

        [JsonPropertyName("TaxIdentifier")]
        [MaxLength(50)]
        public string? TaxIdentifier { get; set; }

        [JsonPropertyName("AcctNum")]
        [MaxLength(100)]
        public string? AcctNum { get; set; }

        [JsonPropertyName("CompanyName")]
        [MaxLength(500)]
        public string? CompanyName { get; set; }

        [JsonPropertyName("BillAddr")]
        public VendorBillAddr? BillAddr { get; set; }

        [JsonPropertyName("GivenName")]
        [MaxLength(100)]
        public string? GivenName { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        [MaxLength(500)]
        public string? PrintOnCheckName { get; set; }
    }

    public class VendorEmailAddr
    {
        [JsonPropertyName("Address")]
        [EmailAddress]
        [MaxLength(100)]
        public string? Address { get; set; }
    }

    public class VendorPhone
    {
        [JsonPropertyName("FreeFormNumber")]
        [MaxLength(50)]
        public string? FreeFormNumber { get; set; }
    }

    public class VendorWebAddr
    {
        [JsonPropertyName("URI")]
        [MaxLength(500)]
        public string? URI { get; set; }
    }

    public class VendorBillAddr
    {
        [JsonPropertyName("City")]
        [MaxLength(255)]
        public string? City { get; set; }

        [JsonPropertyName("Country")]
        [MaxLength(255)]
        public string? Country { get; set; }

        [JsonPropertyName("Line3")]
        [MaxLength(500)]
        public string? Line3 { get; set; }

        [JsonPropertyName("Line2")]
        [MaxLength(500)]
        public string? Line2 { get; set; }

        [JsonPropertyName("Line1")]
        [MaxLength(500)]
        public string? Line1 { get; set; }

        [JsonPropertyName("PostalCode")]
        [MaxLength(30)]
        public string? PostalCode { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        [MaxLength(255)]
        public string? CountrySubDivisionCode { get; set; }
    }
}
