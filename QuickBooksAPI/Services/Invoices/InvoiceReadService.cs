using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services.Invoices;

public sealed class InvoiceReadService : IInvoiceReadService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceReadService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
    }

    public async Task<ApiResponse<IEnumerable<QBOInvoiceHeader>>> ListAsync(string realmId)
    {
        var invoices = await _invoiceRepository.GetAllByRealmAsync(realmId);
        return ApiResponse<IEnumerable<QBOInvoiceHeader>>.Ok(invoices);
    }

    public async Task<ApiResponse<PagedResult<QBOInvoiceHeader>>> ListPagedAsync(string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _invoiceRepository.GetPagedByRealmAsync(realmId, page, pageSize, search);
        return ApiResponse<PagedResult<QBOInvoiceHeader>>.Ok(result);
    }
}
