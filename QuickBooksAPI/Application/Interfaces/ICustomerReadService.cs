using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICustomerReadService
{
    Task<ApiResponse<IEnumerable<Customer>>> ListAsync(int userId, string realmId);

    Task<ApiResponse<PagedResult<Customer>>> ListPagedAsync(int userId, string realmId, ListQueryParams query);

    Task<ApiResponse<Customer>> GetByQboIdAsync(int userId, string realmId, string qboId);
}
