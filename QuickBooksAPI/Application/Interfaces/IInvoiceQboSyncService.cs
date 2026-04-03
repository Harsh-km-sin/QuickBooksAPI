using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IInvoiceQboSyncService
{
    Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId);
}
