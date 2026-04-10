using System.Text.Json;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services.Sync;
using Microsoft.Extensions.Logging;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Services.Vendors;

public sealed class VendorQboSyncService : IVendorQboSyncService
{
    private readonly IAuthService _authService;
    private readonly IVendorAccountingSyncGateway _vendorSyncGateway;
    private readonly IVendorRepository _vendorRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly ILogger<VendorQboSyncService> _logger;

    public VendorQboSyncService(
        IAuthService authService,
        IVendorAccountingSyncGateway vendorSyncGateway,
        IVendorRepository vendorRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        ILogger<VendorQboSyncService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _vendorSyncGateway = vendorSyncGateway ?? throw new ArgumentNullException(nameof(vendorSyncGateway));
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        _qboSyncStateRepository = qboSyncStateRepository ?? throw new ArgumentNullException(nameof(qboSyncStateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            const int PageSize = 1000;
            var startPosition = 1;
            var totalSynced = 0;

            var lastUpdatedAfter = await _qboSyncStateRepository
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Vendors.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            lastUpdatedAfter = QboSyncTimeHelper.NormalizeLastUpdatedAfterFromDb(lastUpdatedAfter);

            DateTime? maxUpdatedTime = null;

            while (true)
            {
                var json = await _vendorSyncGateway.FetchVendorsPageAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter);

                var qbo = JsonSerializer.Deserialize<QuickBooksVendorQueryResponse>(json);
                var vendors = qbo?.QueryResponse?.Vendor;

                if (vendors == null || vendors.Count == 0)
                    break;

                foreach (var vendor in vendors)
                {
                    if (vendor.MetaData?.LastUpdatedTime != null)
                    {
                        var dtoLastUpdated = vendor.MetaData.LastUpdatedTime;
                        var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                            ? dtoLastUpdated
                            : dtoLastUpdated.ToUniversalTime();

                        if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            maxUpdatedTime = dtoLastUpdatedUtc;
                    }
                }

                var vendorModels = vendors.Select(v => QuickBooksVendorMapper.Map(v, userId, realmId)).ToList();
                await _vendorRepository.UpsertVendorsAsync(vendorModels, userId, realmId);

                totalSynced += vendors.Count;

                if (vendors.Count < PageSize)
                    break;

                startPosition += PageSize;
            }

            if (totalSynced > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = QboSyncTimeHelper.ClampFutureSyncTimestampUtc(maxUpdatedTime.Value);

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Vendors.ToString(),
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Vendors.ToString(),
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} vendors.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vendor sync failed for user {UserId}", userId);
            return ApiResponse<int>.Fail("Failed to sync vendors.", new[] { ex.Message });
        }
    }
}
