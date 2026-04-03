using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IBillReadService
{
    Task<ApiResponse<IEnumerable<QBOBillHeader>>> ListAsync(string realmId);

    Task<ApiResponse<PagedResult<QBOBillHeader>>> ListPagedAsync(string realmId, ListQueryParams query);

    Task<ApiResponse<QBOBillHeader>> GetByIdAsync(string realmId, string id);
}
