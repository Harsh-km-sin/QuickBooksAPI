using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksCompanyInfoResponse
    {
        [JsonPropertyName("CompanyInfo")]
        public QuickBooksCompanyInfo CompanyInfo { get; set; } = null!;
    }

    public class QuickBooksCompanyInfo
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("LegalName")]
        public string? LegalName { get; set; }
    }
}

