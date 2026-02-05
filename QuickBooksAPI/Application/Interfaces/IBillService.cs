using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IBillService
    {
        Task<ApiResponse<int>> SyncBillsAsync();
        Task<ApiResponse<string>> CreateBillAsync(CreateBillRequest request);
        Task<ApiResponse<string>> UpdateBillAsync(UpdateBillRequest request);
        Task<ApiResponse<string>> DeleteBillAsync(DeleteBillRequest request);
    }
}
