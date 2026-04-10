using System.Text.Json;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Bills;

public sealed class BillQboSyncService : IBillQboSyncService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksBillService _quickBooksBillService;
    private readonly IBillRepository _billRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly ILogger<BillQboSyncService> _logger;

    public BillQboSyncService(
        IAuthService authService,
        IQuickBooksBillService quickBooksBillService,
        IBillRepository billRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        ILogger<BillQboSyncService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksBillService = quickBooksBillService ?? throw new ArgumentNullException(nameof(quickBooksBillService));
        _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
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
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Bills.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            if (lastUpdatedAfter.HasValue && lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);

            DateTime? maxUpdatedTime = null;

            while (true)
            {
                var json = await _quickBooksBillService.GetBillsAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter);

                var qbo = JsonSerializer.Deserialize<QuickBooksBillQueryResponse>(json);
                var bills = qbo?.QueryResponse?.Bill;

                if (bills == null || bills.Count == 0)
                    break;

                using var conn = _billRepository.CreateOpenConnection();
                using var tx = conn.BeginTransaction();

                try
                {
                    var pageHeaders = new List<QBOBillHeader>();
                    var pageLines = new List<BillLineUpsertRow>();

                    foreach (var bill in bills)
                    {
                        if (bill.MetaData?.LastUpdatedTime != null)
                        {
                            var dtoLastUpdated = bill.MetaData.LastUpdatedTime;
                            var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();
                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                maxUpdatedTime = dtoLastUpdatedUtc;
                        }

                        pageHeaders.Add(QuickBooksBillMapper.MapToHeader(bill, realmId));
                        pageLines.AddRange(QuickBooksBillMapper.MapToLineUpsertRows(bill, realmId));
                        totalSynced++;
                    }

                    await _billRepository.UpsertBillsAsync(pageHeaders, pageLines, conn, tx);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                startPosition += PageSize;
            }

            if (totalSynced > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = maxUpdatedTime.Value;
                var nowUtc = DateTime.UtcNow;
                if (timeToStore > nowUtc.AddSeconds(30))
                    timeToStore = nowUtc;
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Bills.ToString(),
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Bills.ToString(),
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} bills.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bill sync failed for user {UserId} realm {RealmId}", userId, realmId);
            return ApiResponse<int>.Fail("Failed to sync bills from QuickBooks.", new[] { ex.Message });
        }
    }
}
