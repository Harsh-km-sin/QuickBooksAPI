using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IInvoiceReadService
{
    Task<ApiResponse<IEnumerable<InvoiceListItemDto>>> ListAsync(string realmId);

    Task<ApiResponse<PagedResult<InvoiceListItemDto>>> ListPagedAsync(string realmId, ListQueryParams query);
}
