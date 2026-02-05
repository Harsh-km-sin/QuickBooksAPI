using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<ApiResponse<int>> GetCustomersAsync();
        Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request);
        Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request);
        Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request);
    }
}
