using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IBillReadService
{
    Task<ApiResponse<IEnumerable<BillListItemDto>>> ListAsync(string realmId);

    Task<ApiResponse<PagedResult<BillListItemDto>>> ListPagedAsync(string realmId, ListQueryParams query);

    Task<ApiResponse<BillListItemDto>> GetByIdAsync(string realmId, string id);
}
