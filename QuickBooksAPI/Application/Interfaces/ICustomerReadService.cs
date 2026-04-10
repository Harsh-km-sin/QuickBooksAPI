using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICustomerReadService
{
    Task<ApiResponse<IEnumerable<CustomerDto>>> ListAsync(int userId, string realmId);

    Task<ApiResponse<PagedResult<CustomerDto>>> ListPagedAsync(int userId, string realmId, ListQueryParams query);

    Task<ApiResponse<CustomerDto>> GetByQboIdAsync(int userId, string realmId, string qboId);
}
