using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IInvoiceReadService
{
    Task<ApiResponse<IEnumerable<QBOInvoiceHeader>>> ListAsync(string realmId);

    Task<ApiResponse<PagedResult<QBOInvoiceHeader>>> ListPagedAsync(string realmId, ListQueryParams query);
}
