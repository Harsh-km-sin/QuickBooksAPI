using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IChartOfAccountsService
    {
        Task<ApiResponse<IEnumerable<ChartOfAccounts>>> ListChartOfAccountsAsync();
        Task<ApiResponse<int>> syncChartOfAccounts();
    }
}
