using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class DeleteProductRequest
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }

        [JsonPropertyName("sparse")]
        public bool Sparse { get; set; } = true;

        public bool Active { get; set; } = false;
    }
}
