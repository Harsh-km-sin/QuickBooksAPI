using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Customers.Handlers;

namespace QuickBooksAPI.Features.Customers;

/// <summary>MIGRATION-ONLY: <see cref="ICustomerService"/> implemented via feature handlers.</summary>
public sealed class CustomerServiceMigrationFacade : ICustomerService
{
    private readonly ListCustomersHandler _list;
    private readonly SyncCustomersHandler _sync;
    private readonly CreateCustomerHandler _create;
    private readonly UpdateCustomerHandler _update;
    private readonly DeleteCustomerHandler _delete;

    public CustomerServiceMigrationFacade(
        ListCustomersHandler list,
        SyncCustomersHandler sync,
        CreateCustomerHandler create,
        UpdateCustomerHandler update,
        DeleteCustomerHandler delete)
    {
        _list = list;
        _sync = sync;
        _create = create;
        _update = update;
        _delete = delete;
    }

    public Task<ApiResponse<int>> GetCustomersAsync() => _sync.HandleAsync();
    public Task<ApiResponse<IEnumerable<CustomerDto>>> ListCustomersAsync() => _list.HandleListAsync();
    public Task<ApiResponse<PagedResult<CustomerDto>>> ListCustomersAsync(ListQueryParams query) => _list.HandlePagedAsync(query);
    public Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id) => _list.HandleGetByIdAsync(id);
    public Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request) => _create.HandleAsync(request);
    public Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request) => _update.HandleAsync(request);
    public Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request) => _delete.HandleAsync(request);
}
