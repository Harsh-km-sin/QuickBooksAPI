using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Services;

/// <summary>
/// API façade for vendors; logic in <c>QuickBooksAPI.Services.Vendors</c>.
/// </summary>
public sealed class VendorService : IVendorService
{
    private readonly IRequestContext _requestContext;
    private readonly IVendorReadService _read;
    private readonly IVendorQboSyncService _sync;
    private readonly IVendorQboCommandService _commands;

    public VendorService(
        IRequestContext requestContext,
        IVendorReadService read,
        IVendorQboSyncService sync,
        IVendorQboCommandService commands)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _read = read ?? throw new ArgumentNullException(nameof(read));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public async Task<ApiResponse<IEnumerable<Vendor>>> ListVendorsAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<IEnumerable<Vendor>>.Fail(err!);
        return await _read.ListAsync(userId, realmId);
    }

    public async Task<ApiResponse<PagedResult<Vendor>>> ListVendorsAsync(ListQueryParams query)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<PagedResult<Vendor>>.Fail(err!);
        return await _read.ListPagedAsync(userId, realmId, query);
    }

    public async Task<ApiResponse<int>> GetVendorsAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<int>.Fail(err!);
        return await _sync.SyncFromQuickBooksAsync(userId, realmId);
    }

    public async Task<ApiResponse<string>> CreateVendorAsync(CreateVendorRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.CreateAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> SoftDeleteVendorAsync(SoftDeleteVendorRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.SoftDeleteAsync(userId, realmId, request);
    }

    public async Task<ApiResponse<string>> UpdatevendorAsync(UpdateVendorRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.UpdateAsync(userId, realmId, request);
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
