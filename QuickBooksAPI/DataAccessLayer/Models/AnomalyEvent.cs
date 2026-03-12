namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class AnomalyEvent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
