using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Invoices.Handlers;

public sealed class ListInvoicesHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IInvoiceReadService _read;

    public ListInvoicesHandler(IRequestContext requestContext, IInvoiceReadService read)
    {
        _requestContext = requestContext;
        _read = read;
    }

    public async Task<ApiResponse<IEnumerable<InvoiceListItemDto>>> HandleListAsync()
    {
        if (!FeatureRequestContextGuard.TryGetRealm(_requestContext, out var realmId, out var err))
            return ApiResponse<IEnumerable<InvoiceListItemDto>>.Fail(err!);
        return await _read.ListAsync(realmId);
    }

    public async Task<ApiResponse<PagedResult<InvoiceListItemDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (!FeatureRequestContextGuard.TryGetRealm(_requestContext, out var realmId, out var err))
            return ApiResponse<PagedResult<InvoiceListItemDto>>.Fail(err!);
        return await _read.ListPagedAsync(realmId, query);
    }
}
