using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Vendors.Handlers;

namespace QuickBooksAPI.Features.Vendors;

/// <summary>MIGRATION-ONLY: <see cref="IVendorService"/> via feature handlers.</summary>
public sealed class VendorServiceMigrationFacade : IVendorService
{
    private readonly ListVendorsHandler _list;
    private readonly SyncVendorsHandler _sync;
    private readonly CreateVendorHandler _create;
    private readonly UpdateVendorHandler _update;
    private readonly SoftDeleteVendorHandler _softDelete;

    public VendorServiceMigrationFacade(
        ListVendorsHandler list,
        SyncVendorsHandler sync,
        CreateVendorHandler create,
        UpdateVendorHandler update,
        SoftDeleteVendorHandler softDelete)
    {
        _list = list;
        _sync = sync;
        _create = create;
        _update = update;
        _softDelete = softDelete;
    }

    public Task<ApiResponse<IEnumerable<VendorDto>>> ListVendorsAsync() => _list.HandleListAsync();
    public Task<ApiResponse<PagedResult<VendorDto>>> ListVendorsAsync(ListQueryParams query) => _list.HandlePagedAsync(query);
    public Task<ApiResponse<int>> GetVendorsAsync() => _sync.HandleAsync();
    public Task<ApiResponse<string>> CreateVendorAsync(CreateVendorRequest request) => _create.HandleAsync(request);
    public Task<ApiResponse<string>> UpdatevendorAsync(UpdateVendorRequest request) => _update.HandleAsync(request);
    public Task<ApiResponse<string>> SoftDeleteVendorAsync(SoftDeleteVendorRequest request) => _softDelete.HandleAsync(request);
}
