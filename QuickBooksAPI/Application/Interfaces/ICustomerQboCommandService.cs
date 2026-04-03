using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICustomerQboCommandService
{
    Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateCustomerRequest request);

    Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateCustomerRequest request);

    Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteCustomerRequest request);
}
