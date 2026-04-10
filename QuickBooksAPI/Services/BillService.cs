using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services;

/// <summary>
/// API façade for bills; logic in <c>QuickBooksAPI.Services.Bills</c>.
/// </summary>
public sealed class BillService : IBillService
{
    private readonly IRequestContext _requestContext;
    private readonly IBillReadService _read;
    private readonly IBillQboSyncService _sync;
    private readonly IBillQboCommandService _commands;

    public BillService(
        IRequestContext requestContext,
        IBillReadService read,
        IBillQboSyncService sync,
        IBillQboCommandService commands)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _read = read ?? throw new ArgumentNullException(nameof(read));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public async Task<ApiResponse<IEnumerable<BillListItemDto>>> ListBillsAsync()
    {
        if (!TryGetRealm(out var realmId, out var err))
            return ApiResponse<IEnumerable<BillListItemDto>>.Fail(err!);
        return await _read.ListAsync(realmId);
    }

    public async Task<ApiResponse<PagedResult<BillListItemDto>>> ListBillsAsync(ListQueryParams query)
    {
        if (!TryGetRealm(out var realmId, out var err))
            return ApiResponse<PagedResult<BillListItemDto>>.Fail(err!);
        return await _read.ListPagedAsync(realmId, query);
    }

    public async Task<ApiResponse<BillListItemDto>> GetBillByIdAsync(string id)
    {
        if (!TryGetRealm(out var realmId, out var err))
            return ApiResponse<BillListItemDto>.Fail(err!);
        return await _read.GetByIdAsync(realmId, id);
    }

    public async Task<ApiResponse<int>> SyncBillsAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId);
    }

    public async Task<ApiResponse<string>> CreateBillAsync(CreateBillRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.CreateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> UpdateBillAsync(UpdateBillRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.UpdateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> DeleteBillAsync(DeleteBillRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.DeleteAsync(userId, realmId, request);
    }

    private bool TryGetRealm(out string realmId, out string? error)
    {
        realmId = string.Empty;
        error = null;
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
        {
            error = "User context is missing. Please sign in and connect QuickBooks.";
            return false;
        }

        realmId = _requestContext.RealmId;
        return true;
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
