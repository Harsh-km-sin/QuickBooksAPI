using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Bills.Handlers;

namespace QuickBooksAPI.Features.Bills;

/// <summary>MIGRATION-ONLY: <see cref="IBillService"/> via feature handlers.</summary>
public sealed class BillServiceMigrationFacade : IBillService
{
    private readonly ListBillsHandler _list;
    private readonly SyncBillsHandler _sync;
    private readonly CreateBillHandler _create;
    private readonly UpdateBillHandler _update;
    private readonly DeleteBillHandler _delete;

    public BillServiceMigrationFacade(
        ListBillsHandler list,
        SyncBillsHandler sync,
        CreateBillHandler create,
        UpdateBillHandler update,
        DeleteBillHandler delete)
    {
        _list = list;
        _sync = sync;
        _create = create;
        _update = update;
        _delete = delete;
    }

    public Task<ApiResponse<IEnumerable<BillListItemDto>>> ListBillsAsync() => _list.HandleListAsync();
    public Task<ApiResponse<PagedResult<BillListItemDto>>> ListBillsAsync(ListQueryParams query) => _list.HandlePagedAsync(query);
    public Task<ApiResponse<BillListItemDto>> GetBillByIdAsync(string id) => _list.HandleGetByIdAsync(id);
    public Task<ApiResponse<int>> SyncBillsAsync() => _sync.HandleAsync();
    public Task<ApiResponse<string>> CreateBillAsync(CreateBillRequest request) => _create.HandleAsync(request);
    public Task<ApiResponse<string>> UpdateBillAsync(UpdateBillRequest request) => _update.HandleAsync(request);
    public Task<ApiResponse<string>> DeleteBillAsync(DeleteBillRequest request) => _delete.HandleAsync(request);
}
