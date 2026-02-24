namespace QuickBooksAPI.Application.Interfaces
{
    public interface ISyncService
    {
        Task StartFullSyncAsync(string companyId, string userId);
        Task<SyncStatusDto?> GetSyncStatusAsync(string companyId);
    }

    public class SyncStatusDto
    {
        public string CompanyId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? LastRun { get; set; }
        public string? Error { get; set; }
    }
}
