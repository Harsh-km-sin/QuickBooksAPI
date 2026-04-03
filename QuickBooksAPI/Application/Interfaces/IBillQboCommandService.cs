using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IBillQboCommandService
{
    Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateBillRequest request);

    Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateBillRequest request);

    Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteBillRequest request);
}
