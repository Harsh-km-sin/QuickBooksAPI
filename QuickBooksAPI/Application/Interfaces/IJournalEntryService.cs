using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IJournalEntryService
    {
        Task<ApiResponse<int>> SyncJournalEntriesAsync();
    }
}
