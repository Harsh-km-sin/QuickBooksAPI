using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksItemQueryResponse
    {
        [JsonPropertyName("QueryResponse")]
        public ItemQueryResponse QueryResponse { get; set; } = null!;
    }

    public class ItemQueryResponse
    {
        [JsonPropertyName("Item")]
        public List<QuickBooksItemDto>? Items { get; set; }
    }

    public class QuickBooksItemMutationResponse
    {
        [JsonPropertyName("Item")]
        public QuickBooksItemDto Item { get; set; } = null!;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    public class QuickBooksItemDto
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        [JsonPropertyName("FullyQualifiedName")]
        public string FullyQualifiedName { get; set; } = null!;

        [JsonPropertyName("Taxable")]
        public bool Taxable { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("Type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("QtyOnHand")]
        public decimal? QtyOnHand { get; set; }

        [JsonPropertyName("IncomeAccountRef")]
        public IncomeAccountRef? IncomeAccountRef { get; set; }

        [JsonPropertyName("PurchaseCost")]
        public decimal PurchaseCost { get; set; }

        [JsonPropertyName("TrackQtyOnHand")]
        public bool TrackQtyOnHand { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = null!;

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; }

        [JsonPropertyName("Id")]
        public string QBOId { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("MetaData")]
        public ItemMetaData MetaData { get; set; } = null!;
    }

    public class IncomeAccountRef
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class ItemMetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }
    }

}
