using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<ApiResponse<int>> GetCustomersAsync();
        Task<ApiResponse<IEnumerable<CustomerDto>>> ListCustomersAsync();
        Task<ApiResponse<PagedResult<CustomerDto>>> ListCustomersAsync(ListQueryParams query);
        Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request);
        Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request);
        Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request);
        Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id);
    }
}
