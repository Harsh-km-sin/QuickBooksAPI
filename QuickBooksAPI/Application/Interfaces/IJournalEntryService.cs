using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IJournalEntryService
    {
        Task<ApiResponse<IEnumerable<QBOJournalEntryHeader>>> ListJournalEntriesAsync();
        Task<ApiResponse<int>> SyncJournalEntriesAsync();
    }
}
