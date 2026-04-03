using System.Text.Json;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Invoices;

public sealed class InvoiceQboSyncService : IInvoiceQboSyncService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksInvoiceService _quickBooksInvoiceService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly ILogger<InvoiceQboSyncService> _logger;

    public InvoiceQboSyncService(
        IAuthService authService,
        IQuickBooksInvoiceService quickBooksInvoiceService,
        IInvoiceRepository invoiceRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        ILogger<InvoiceQboSyncService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksInvoiceService = quickBooksInvoiceService ?? throw new ArgumentNullException(nameof(quickBooksInvoiceService));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
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
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Invoice.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            if (lastUpdatedAfter.HasValue && lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);

            DateTime? maxUpdatedTime = null;

            while (true)
            {
                var json = await _quickBooksInvoiceService.GetInvoiceAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter);

                var qbo = JsonSerializer.Deserialize<QuickBooksInvoiceQueryResponse>(json);
                var invoices = qbo?.QueryResponse?.Invoice;

                if (invoices == null || invoices.Count == 0)
                    break;

                using var conn = _invoiceRepository.CreateOpenConnection();
                using var tx = conn.BeginTransaction();

                try
                {
                    var pageHeaders = new List<QBOInvoiceHeader>();
                    var pageLines = new List<InvoiceLineUpsertRow>();

                    foreach (var inv in invoices)
                    {
                        if (inv.MetaData?.LastUpdatedTime != null)
                        {
                            var dtoLastUpdated = inv.MetaData.LastUpdatedTime;
                            var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();
                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                maxUpdatedTime = dtoLastUpdatedUtc;
                        }

                        pageHeaders.Add(QuickBooksInvoiceMapper.MapToHeader(inv, realmId));
                        pageLines.AddRange(QuickBooksInvoiceMapper.MapToLineUpsertRows(inv, realmId));
                        totalSynced++;
                    }

                    await _invoiceRepository.UpsertInvoicesAsync(pageHeaders, pageLines, conn, tx);
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
                    QboEntityType.Invoice.ToString(),
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Invoice.ToString(),
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} invoices.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice sync failed for user {UserId}", userId);
            return ApiResponse<int>.Fail("Failed to sync invoices.", new[] { ex.Message });
        }
    }
}
