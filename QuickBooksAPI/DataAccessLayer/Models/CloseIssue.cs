namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class CloseIssue
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string? Details { get; set; }
        public DateTime DetectedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
