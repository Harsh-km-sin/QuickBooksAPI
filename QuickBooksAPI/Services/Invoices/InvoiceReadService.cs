using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;

namespace QuickBooksAPI.Services.Invoices;

public sealed class InvoiceReadService : IInvoiceReadService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceReadService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
    }

    public async Task<ApiResponse<IEnumerable<InvoiceListItemDto>>> ListAsync(string realmId)
    {
        var invoices = await _invoiceRepository.GetAllByRealmAsync(realmId);
        return ApiResponse<IEnumerable<InvoiceListItemDto>>.Ok(invoices.Select(InvoiceBillReadMapping.ToInvoiceDto));
    }

    public async Task<ApiResponse<InvoiceListItemDto>> GetByIdAsync(string realmId, string id)
    {
        var invoice = await _invoiceRepository.GetByQbIdAsync(id, realmId);
        if (invoice == null)
        {
            return ApiResponse<InvoiceListItemDto>.Fail($"Invoice with ID '{id}' was not found.");
        }
        return ApiResponse<InvoiceListItemDto>.Ok(InvoiceBillReadMapping.ToInvoiceDto(invoice));
    }

    public async Task<ApiResponse<PagedResult<InvoiceListItemDto>>> ListPagedAsync(string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _invoiceRepository.GetPagedByRealmAsync(realmId, page, pageSize, search, query.SortBy, query.IsDescending());
        return ApiResponse<PagedResult<InvoiceListItemDto>>.Ok(InvoiceBillReadMapping.ToInvoiceDtoPaged(result));
    }
}
