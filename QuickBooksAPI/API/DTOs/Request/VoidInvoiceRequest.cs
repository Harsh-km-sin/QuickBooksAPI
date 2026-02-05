using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Matches official QuickBooks API "Void an invoice" request body.
    /// Ref: https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/invoice
    /// </summary>
    public class VoidInvoiceRequest
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
