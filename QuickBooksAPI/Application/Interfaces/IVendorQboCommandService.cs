using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IVendorQboCommandService
{
    Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateVendorRequest request);

    Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateVendorRequest request);

    Task<ApiResponse<string>> SoftDeleteAsync(int userId, string realmId, SoftDeleteVendorRequest request);
}
