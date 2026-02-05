using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches QuickBooks API "Delete a bill" request body.
    /// </summary>
    public class DeleteBillRequest
    {
        [JsonPropertyName("SyncToken")]
        [Required]
        [MaxLength(50)]
        public string SyncToken { get; set; } = null!;

        [JsonPropertyName("Id")]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = null!;
    }
}
