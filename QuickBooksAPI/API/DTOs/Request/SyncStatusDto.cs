namespace QuickBooksAPI.API.DTOs.Request
{
    public class SyncStatusDto
    {
        public string CompanyId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? LastRun { get; set; }
        public string? Error { get; set; }
    }
}
