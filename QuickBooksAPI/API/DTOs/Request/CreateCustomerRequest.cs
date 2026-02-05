using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class CreateCustomerRequest
    {
        [JsonPropertyName("GivenName")]
        [MaxLength(100)]
        public string? GivenName { get; set; }

        [JsonPropertyName("MiddleName")]
        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [JsonPropertyName("FamilyName")]
        [MaxLength(100)]
        public string? FamilyName { get; set; }

        [JsonPropertyName("Title")]
        [MaxLength(50)]
        public string? Title { get; set; }

        [JsonPropertyName("Suffix")]
        [MaxLength(50)]
        public string? Suffix { get; set; }

        [JsonPropertyName("DisplayName")]
        [Required]
        [MaxLength(500)]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("FullyQualifiedName")]
        public string? FullyQualifiedName { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public CreateEmailDto? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public CreatePhoneDto? PrimaryPhone { get; set; }

        [JsonPropertyName("BillAddr")]
        public CreateAddressDto? BillAddr { get; set; }
    }

    public class CreateEmailDto
    {
        [JsonPropertyName("Address")]
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Address { get; set; } = null!;
    }

    public class CreatePhoneDto
    {
        [JsonPropertyName("FreeFormNumber")]
        [Required]
        [MaxLength(50)]
        public string FreeFormNumber { get; set; } = null!;
    }

    public class CreateAddressDto
    {
        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }
    }
}
