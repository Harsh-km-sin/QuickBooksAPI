using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Services;

/// <summary>
/// API façade for invoices; logic in <c>QuickBooksAPI.Services.Invoices</c>.
/// </summary>
public sealed class InvoiceService : IInvoiceService
{
    private readonly IRequestContext _requestContext;
    private readonly IInvoiceReadService _read;
    private readonly IInvoiceQboSyncService _sync;
    private readonly IInvoiceQboCommandService _commands;

    public InvoiceService(
        IRequestContext requestContext,
        IInvoiceReadService read,
        IInvoiceQboSyncService sync,
        IInvoiceQboCommandService commands)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _read = read ?? throw new ArgumentNullException(nameof(read));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public async Task<ApiResponse<IEnumerable<QBOInvoiceHeader>>> ListInvoicesAsync()
    {
        if (!TryGetRealm(out var realmId, out var err))
            return ApiResponse<IEnumerable<QBOInvoiceHeader>>.Fail(err!);
        return await _read.ListAsync(realmId);
    }

    public async Task<ApiResponse<PagedResult<QBOInvoiceHeader>>> ListInvoicesAsync(ListQueryParams query)
    {
        if (!TryGetRealm(out var realmId, out var err))
            return ApiResponse<PagedResult<QBOInvoiceHeader>>.Fail(err!);
        return await _read.ListPagedAsync(realmId, query);
    }

    public async Task<ApiResponse<int>> SyncInvoicesAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId);
    }

    public async Task<ApiResponse<string>> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.CreateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> UpdateInvoiceAsync(UpdateInvoiceRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.UpdateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> DeleteInvoiceAsync(DeleteInvoiceRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.DeleteAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> VoidInvoiceAsync(VoidInvoiceRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.VoidAsync(userId, realmId, request);
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
