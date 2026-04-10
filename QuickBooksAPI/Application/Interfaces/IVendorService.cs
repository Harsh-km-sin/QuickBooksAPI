using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IVendorService
    {
        Task<ApiResponse<IEnumerable<VendorDto>>> ListVendorsAsync();
        Task<ApiResponse<PagedResult<VendorDto>>> ListVendorsAsync(ListQueryParams query);
        Task<ApiResponse<int>> GetVendorsAsync();
        Task<ApiResponse<string>> CreateVendorAsync(CreateVendorRequest request);
        Task<ApiResponse<string>> UpdatevendorAsync(UpdateVendorRequest request);
        Task<ApiResponse<string>> SoftDeleteVendorAsync(SoftDeleteVendorRequest request);
    }
}
