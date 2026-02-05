using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IVendorService
    {
        Task<ApiResponse<int>> GetVendorsAsync();
        Task<ApiResponse<string>> CreateVendorAsync(CreateVendorRequest request);
        Task<ApiResponse<string>> UpdatevendorAsync(UpdateVendorRequest request);
        Task<ApiResponse<string>> SoftDeleteVendorAsync(SoftDeleteVendorRequest request);
    }
}
