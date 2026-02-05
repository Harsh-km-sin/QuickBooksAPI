using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class UpdateCustomerRequest
    {
        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = default!;

        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = default!;

        // For partial updates set true, otherwise false
        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; } = false;

        [JsonPropertyName("domain")]
        public string? Domain { get; set; } = "QBO";

        [JsonPropertyName("PrimaryEmailAddr")]
        public PrimaryEmailAddr? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("DisplayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("PreferredDeliveryMethod")]
        public string? PreferredDeliveryMethod { get; set; }

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FullyQualifiedName")]
        public string? FullyQualifiedName { get; set; }

        [JsonPropertyName("BillWithParent")]
        public bool? BillWithParent { get; set; }

        [JsonPropertyName("Job")]
        public bool? Job { get; set; }

        [JsonPropertyName("BalanceWithJobs")]
        public double? BalanceWithJobs { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public PrimaryPhone? PrimaryPhone { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("MetaData")]
        public JournalEntryMetaData? MetaData { get; set; }

        [JsonPropertyName("BillAddr")]
        public BillAddr? BillAddr { get; set; }

        [JsonPropertyName("MiddleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("Taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("Balance")]
        public double? Balance { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }
    }

    public class BillAddr
    {
        public string? City { get; set; }
        public string? Line1 { get; set; }
        public string? PostalCode { get; set; }
        public string? Lat { get; set; }
        public string? Long { get; set; }
        public string? CountrySubDivisionCode { get; set; }
        public string? Id { get; set; }
    }

    public class JournalEntryMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }
    }
}
