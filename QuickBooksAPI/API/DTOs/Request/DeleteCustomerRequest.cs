using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class DeleteCustomerRequest
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }
        [JsonPropertyName("SyncToken")]
        public string SyncToken { get; set; }
        [JsonPropertyName("sparse")]
        public bool sparse { get; set; }
        [JsonPropertyName("Active")]
        public bool Active { get; set; }
    }
}
