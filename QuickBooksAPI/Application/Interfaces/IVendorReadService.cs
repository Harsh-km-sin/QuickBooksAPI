using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Application.Interfaces;

public interface IVendorReadService
{
    Task<ApiResponse<IEnumerable<Vendor>>> ListAsync(int userId, string realmId);

    Task<ApiResponse<PagedResult<Vendor>>> ListPagedAsync(int userId, string realmId, ListQueryParams query);
}
