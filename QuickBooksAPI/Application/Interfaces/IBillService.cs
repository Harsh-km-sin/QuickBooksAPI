using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IBillService
    {
        Task<ApiResponse<IEnumerable<BillListItemDto>>> ListBillsAsync();
        Task<ApiResponse<PagedResult<BillListItemDto>>> ListBillsAsync(ListQueryParams query);
        Task<ApiResponse<int>> SyncBillsAsync();
        Task<ApiResponse<string>> CreateBillAsync(CreateBillRequest request);
        Task<ApiResponse<string>> UpdateBillAsync(UpdateBillRequest request);
        Task<ApiResponse<string>> DeleteBillAsync(DeleteBillRequest request);
        Task<ApiResponse<BillListItemDto>> GetBillByIdAsync(string id);
    }
}
