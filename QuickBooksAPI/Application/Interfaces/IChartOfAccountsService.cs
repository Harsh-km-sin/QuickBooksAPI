using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IChartOfAccountsService
    {
        Task<ApiResponse<int>> syncChartOfAccounts();
    }
}
