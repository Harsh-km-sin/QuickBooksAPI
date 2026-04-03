using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IBillQboSyncService
{
    Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId);
}
