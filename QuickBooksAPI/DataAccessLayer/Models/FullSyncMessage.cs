namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class FullSyncMessage
    {
        public string CompanyId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime RequestedAt { get; set; }
    }
}
