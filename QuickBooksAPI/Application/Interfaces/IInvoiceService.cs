using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<ApiResponse<IEnumerable<InvoiceListItemDto>>> ListInvoicesAsync();
        Task<ApiResponse<PagedResult<InvoiceListItemDto>>> ListInvoicesAsync(ListQueryParams query);
        Task<ApiResponse<int>> SyncInvoicesAsync();
        Task<ApiResponse<string>> CreateInvoiceAsync(CreateInvoiceRequest request);
        Task<ApiResponse<string>> UpdateInvoiceAsync(UpdateInvoiceRequest request);
        Task<ApiResponse<string>> DeleteInvoiceAsync(DeleteInvoiceRequest request);
        Task<ApiResponse<string>> VoidInvoiceAsync(VoidInvoiceRequest request);
    }
}
