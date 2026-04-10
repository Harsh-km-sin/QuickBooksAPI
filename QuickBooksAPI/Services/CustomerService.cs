using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services;

/// <summary>
/// API façade over customer read, QuickBooks sync, and QuickBooks mutations. Heavy logic lives in <c>QuickBooksAPI.Services.Customers</c>.
/// </summary>
public sealed class CustomerService : ICustomerService
{
    private readonly IRequestContext _requestContext;
    private readonly ICustomerReadService _read;
    private readonly ICustomerQboSyncService _sync;
    private readonly ICustomerQboCommandService _commands;

    public CustomerService(
        IRequestContext requestContext,
        ICustomerReadService read,
        ICustomerQboSyncService sync,
        ICustomerQboCommandService commands)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _read = read ?? throw new ArgumentNullException(nameof(read));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public async Task<ApiResponse<IEnumerable<CustomerDto>>> ListCustomersAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<IEnumerable<CustomerDto>>.Fail(err!);
        return await _read.ListAsync(userId, realmId);
    }

    public async Task<ApiResponse<PagedResult<CustomerDto>>> ListCustomersAsync(ListQueryParams query)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<PagedResult<CustomerDto>>.Fail(err!);
        return await _read.ListPagedAsync(userId, realmId, query);
    }

    public async Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<CustomerDto>.Fail(err!);
        return await _read.GetByQboIdAsync(userId, realmId, id);
    }

    public async Task<ApiResponse<int>> GetCustomersAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId);
    }

    public async Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.CreateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.UpdateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.DeleteAsync(userId, realmId, request);
    }

    private bool TryGetUserRealm(out int userId, out string realmId, out string? error)
    {
        userId = 0;
        realmId = string.Empty;
        error = null;

        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
        {
            error = "User context is missing. Please sign in and connect QuickBooks.";
            return false;
        }

        userId = int.Parse(_requestContext.UserId);
        realmId = _requestContext.RealmId;
        return true;
    }
}
