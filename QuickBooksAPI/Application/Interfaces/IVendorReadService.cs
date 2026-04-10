using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IVendorReadService
{
    Task<ApiResponse<IEnumerable<VendorDto>>> ListAsync(int userId, string realmId);

    Task<ApiResponse<PagedResult<VendorDto>>> ListPagedAsync(int userId, string realmId, ListQueryParams query);
}
