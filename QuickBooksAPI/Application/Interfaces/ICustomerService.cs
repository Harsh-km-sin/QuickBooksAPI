using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<ApiResponse<int>> GetCustomersAsync();
        Task<ApiResponse<IEnumerable<Customer>>> ListCustomersAsync();
        Task<ApiResponse<PagedResult<Customer>>> ListCustomersAsync(ListQueryParams query);
        Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request);
        Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request);
        Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request);
    }
}
