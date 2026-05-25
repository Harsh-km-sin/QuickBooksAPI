using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IChartOfAccountsService
    {
        Task<ApiResponse<IEnumerable<ChartOfAccountsItemDto>>> ListChartOfAccountsAsync();
        Task<ApiResponse<PagedResult<ChartOfAccountsItemDto>>> ListChartOfAccountsAsync(ListQueryParams query);
        Task<ApiResponse<int>> syncChartOfAccounts();
    }
}
