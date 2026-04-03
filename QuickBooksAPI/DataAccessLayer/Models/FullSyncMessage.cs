namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class FullSyncMessage
    {
        public string CompanyId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime RequestedAt { get; set; }

        /// <summary>Optional; when set, propagated to sync scope correlation id for end-to-end tracing.</summary>
        public string? CorrelationId { get; set; }
    }
}
