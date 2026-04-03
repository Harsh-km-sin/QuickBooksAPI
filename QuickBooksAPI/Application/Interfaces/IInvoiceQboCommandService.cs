using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IInvoiceQboCommandService
{
    Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateInvoiceRequest request);

    Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateInvoiceRequest request);

    Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteInvoiceRequest request);

    Task<ApiResponse<string>> VoidAsync(int userId, string realmId, VoidInvoiceRequest request);
}
