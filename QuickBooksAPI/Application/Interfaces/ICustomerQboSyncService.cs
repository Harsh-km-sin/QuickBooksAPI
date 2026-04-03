using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICustomerQboSyncService
{
    Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId);
}
