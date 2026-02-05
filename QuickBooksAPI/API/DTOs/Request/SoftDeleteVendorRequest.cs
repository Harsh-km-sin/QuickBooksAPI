using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    public class SoftDeleteVendorRequest
    {
        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = null!;

        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = null!;
    }
}
