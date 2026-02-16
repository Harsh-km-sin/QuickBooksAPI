using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IChartOfAccountsService
    {
        Task<ApiResponse<IEnumerable<ChartOfAccounts>>> ListChartOfAccountsAsync();
        Task<ApiResponse<PagedResult<ChartOfAccounts>>> ListChartOfAccountsAsync(ListQueryParams query);
        Task<ApiResponse<int>> syncChartOfAccounts();
    }
}
