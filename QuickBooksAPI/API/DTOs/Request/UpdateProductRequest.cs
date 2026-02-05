using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class UpdateProductRequest
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = default!;

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; } = true;

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }   

        [JsonPropertyName("InvStartDate")]
        public string? InvStartDate { get; set; } 

        [JsonPropertyName("QtyOnHand")]
        public decimal? QtyOnHand { get; set; }   

        [JsonPropertyName("AssetAccountRef")]
        public ReferenceType? AssetAccountRef { get; set; }

        [JsonPropertyName("IncomeAccountRef")]
        public ReferenceType? IncomeAccountRef { get; set; }

        [JsonPropertyName("ExpenseAccountRef")]
        public ReferenceType? ExpenseAccountRef { get; set; } 
    }

    public class ReferenceType
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
