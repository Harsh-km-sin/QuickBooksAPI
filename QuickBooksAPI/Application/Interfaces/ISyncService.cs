using QuickBooksAPI.API.DTOs.Request;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ISyncService
    {
        Task StartFullSyncAsync(string companyId, string userId);
        Task<SyncStatusDto?> GetSyncStatusAsync(string companyId);
    }
}
