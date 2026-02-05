using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<ApiResponse<int>> SyncInvoicesAsync();
        Task<ApiResponse<string>> CreateInvoiceAsync(CreateInvoiceRequest request);
        Task<ApiResponse<string>> UpdateInvoiceAsync(UpdateInvoiceRequest request);
        Task<ApiResponse<string>> DeleteInvoiceAsync(DeleteInvoiceRequest request);
        Task<ApiResponse<string>> VoidInvoiceAsync(VoidInvoiceRequest request);
    }
}
