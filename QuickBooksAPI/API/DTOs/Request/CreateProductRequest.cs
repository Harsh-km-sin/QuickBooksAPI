using System.Text.Json.Serialization;
using QuickBooksAPI.DataAccessLayer.DTOs; // For DateOnlyConverter

namespace QuickBooksAPI.API.DTOs.Request
{
    public class CreateProductRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool Active { get; set; } = true;
        public bool TrackQtyOnHand { get; set; }
        public string Type { get; set; } = null!;

        public Reference IncomeAccountRef { get; set; } = null!;
        public Reference ExpenseAccountRef { get; set; } = null!;
        public Reference? AssetAccountRef { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal? PurchaseCost { get; set; }

        public int? QtyOnHand { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? InvStartDate { get; set; }
    }

    public class Reference
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        [JsonPropertyName("value")]
        public string Value { get; set; } = null!;
    }
}
