using QuickBooksAPI.API.DTOs.Request;

namespace QuickBooksAPI.Application.Interfaces;

public interface ISyncStatusRepository
{
    Task<bool> IsRunningAsync(string companyId);
    Task SetStatusAsync(string companyId, string status, string? error = null);
    Task<SyncStatusDto?> GetStatusAsync(string companyId);
}
