using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ISyncStatusRepository
    {
        Task<bool> IsRunningAsync(string companyId);
        Task SetStatusAsync(string companyId, string status, string? error = null);
        Task<SyncStatusDto?> GetStatusAsync(string companyId);
    }
}
