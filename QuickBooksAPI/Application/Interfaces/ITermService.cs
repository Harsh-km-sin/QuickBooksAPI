using System.Collections.Generic;
using System.Threading.Tasks;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ITermService
    {
        Task<IEnumerable<QBOTerm>> GetTermsAsync(string realmId, bool activeOnly = true);
        Task<ApiResponse<int>> SyncTermsAsync(int userId, string realmId);
    }
}
