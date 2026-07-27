using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Invoices.Handlers;

namespace QuickBooksAPI.Features.Invoices;

/// <summary>MIGRATION-ONLY: <see cref="IInvoiceService"/> via feature handlers.</summary>
public sealed class InvoiceServiceMigrationFacade : IInvoiceService
{
    private readonly ListInvoicesHandler _list;
    private readonly SyncInvoicesHandler _sync;
    private readonly CreateInvoiceHandler _create;
    private readonly UpdateInvoiceHandler _update;
    private readonly DeleteInvoiceHandler _delete;
    private readonly VoidInvoiceHandler _void;

    public InvoiceServiceMigrationFacade(
        ListInvoicesHandler list,
        SyncInvoicesHandler sync,
        CreateInvoiceHandler create,
        UpdateInvoiceHandler update,
        DeleteInvoiceHandler delete,
        VoidInvoiceHandler voidHandler)
    {
        _list = list;
        _sync = sync;
        _create = create;
        _update = update;
        _delete = delete;
        _void = voidHandler;
    }

    public Task<ApiResponse<IEnumerable<InvoiceListItemDto>>> ListInvoicesAsync() => _list.HandleListAsync();
    public Task<ApiResponse<PagedResult<InvoiceListItemDto>>> ListInvoicesAsync(ListQueryParams query) => _list.HandlePagedAsync(query);
    public Task<ApiResponse<InvoiceListItemDto>> GetInvoiceByIdAsync(string id) => _list.HandleGetByIdAsync(id);
    public Task<ApiResponse<int>> SyncInvoicesAsync() => _sync.HandleAsync();
    public Task<ApiResponse<string>> CreateInvoiceAsync(CreateInvoiceRequest request) => _create.HandleAsync(request);
    public Task<ApiResponse<string>> UpdateInvoiceAsync(UpdateInvoiceRequest request) => _update.HandleAsync(request);
    public Task<ApiResponse<string>> DeleteInvoiceAsync(DeleteInvoiceRequest request) => _delete.HandleAsync(request);
    public Task<ApiResponse<string>> VoidInvoiceAsync(VoidInvoiceRequest request) => _void.HandleAsync(request);
}
