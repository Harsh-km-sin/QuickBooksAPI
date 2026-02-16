using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace QuickBooksAPI.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IQuickBooksInvoiceService _quickBooksInvoiceService;
        private readonly ITokenRepository _tokenRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IQboSyncStateRepository _iqboSyncStateRepository;
        private readonly IAuthService _authService;

        public InvoiceService(
            ICurrentUser currentUser,
            IQuickBooksInvoiceService quickBooksInvoiceService, 
            ITokenRepository tokenRepository,
            IInvoiceRepository invoiceRepository,
            IQboSyncStateRepository iqboSyncStateRepository,
            IAuthService authService)
        {
            _currentUser = currentUser;
            _quickBooksInvoiceService = quickBooksInvoiceService;
            _tokenRepository = tokenRepository;
            _invoiceRepository = invoiceRepository;
            _iqboSyncStateRepository = iqboSyncStateRepository;
            _authService = authService;
        }

        public async Task<ApiResponse<IEnumerable<QBOInvoiceHeader>>> ListInvoicesAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<QBOInvoiceHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var realmId = _currentUser.RealmId;
            var invoices = await _invoiceRepository.GetAllByRealmAsync(realmId);
            return ApiResponse<IEnumerable<QBOInvoiceHeader>>.Ok(invoices);
        }

        public async Task<ApiResponse<PagedResult<QBOInvoiceHeader>>> ListInvoicesAsync(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<PagedResult<QBOInvoiceHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var realmId = _currentUser.RealmId;
            var page = query.GetPage();
            var pageSize = query.GetPageSize();
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var result = await _invoiceRepository.GetPagedByRealmAsync(realmId, page, pageSize, search);
            return ApiResponse<PagedResult<QBOInvoiceHeader>>.Ok(result);
        }

        public async Task<ApiResponse<int>> SyncInvoicesAsync()
        {
            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            
            // Check and refresh token if expired
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
            {
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });
            }

            const int PageSize = 1000;
            int startPosition = 1;
            int totalSynced = 0;

            var lastUpdatedAfter = await _iqboSyncStateRepository
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Invoice.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            // Ensure LastUpdatedAfter from DB is treated as UTC
            // Use the exact time from the previous sync - QuickBooks ">" query will naturally skip already-synced records
            if (lastUpdatedAfter.HasValue)
            {
                if (lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                {
                    lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);
                }
                // No buffer needed - use exact time, query uses ">" to skip already-synced records
            }

            DateTime? maxUpdatedTime = null; // Track max from actual synced records only

            while (true)
            {
                var json = await _quickBooksInvoiceService.GetInvoiceAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter
                );

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
                        // Track max LastUpdatedTime from synced records
                        if (inv.MetaData?.LastUpdatedTime != null)
                        {
                            var dtoLastUpdated = inv.MetaData.LastUpdatedTime;
                            DateTime dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();
                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                maxUpdatedTime = dtoLastUpdatedUtc;
                        }

                        pageHeaders.Add(MapToHeader(inv, realmId));
                        pageLines.AddRange(MapToLineUpsertRows(inv, realmId));
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

            // Update sync state after successful sync
            if (totalSynced > 0 && maxUpdatedTime.HasValue)
            {
                // Ensure we don't store a time in the future (safeguard against timezone issues)
                var timeToStore = maxUpdatedTime.Value;
                var nowUtc = DateTime.UtcNow;
                if (timeToStore > nowUtc.AddSeconds(30))
                {
                    timeToStore = nowUtc;
                }
                
                // We synced records, use the max LastUpdatedTime from those records (already UTC)
                await _iqboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Invoice.ToString(),
                    timeToStore
                );
            }
            else if (isFirstSync)
            {
                // First sync with no records - mark that we checked
                await _iqboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Invoice.ToString(),
                    DateTime.UtcNow
                );
            }
            // If no records synced and not first sync, don't update sync state (keep previous value)

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} invoices.");
        }

        public async Task<ApiResponse<string>> CreateInvoiceAsync(CreateInvoiceRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var createResponse = await _quickBooksInvoiceService.CreateInvoiceAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(createResponse);
                if (parsed?.Invoice == null)
                    throw new InvalidOperationException("Failed to create invoice in QuickBooks or response is invalid.");

                await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);
                return ApiResponse<string>.Ok(createResponse, "Invoice created successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to create invoice in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> UpdateInvoiceAsync(UpdateInvoiceRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var updateResponse = await _quickBooksInvoiceService.UpdateInvoiceAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(updateResponse);
                if (parsed?.Invoice == null)
                    throw new InvalidOperationException("Failed to update invoice in QuickBooks or response is invalid.");

                await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);
                return ApiResponse<string>.Ok(updateResponse, "Invoice updated successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to update invoice in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> DeleteInvoiceAsync(DeleteInvoiceRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var deleteResponse = await _quickBooksInvoiceService.DeleteInvoiceAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(deleteResponse);
                if (parsed?.Invoice != null)
                    await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);

                return ApiResponse<string>.Ok(deleteResponse, "Invoice deleted successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to delete invoice in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> VoidInvoiceAsync(VoidInvoiceRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var voidResponse = await _quickBooksInvoiceService.VoidInvoiceAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(voidResponse);
                if (parsed?.Invoice != null)
                    await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);

                return ApiResponse<string>.Ok(voidResponse, "Invoice voided successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to void invoice in QuickBooks.", new[] { ex.Message });
            }
        }

        private async Task UpsertSingleInvoiceToDbAsync(QuickBooksInvoiceDto inv, string realmId)
        {
            using var conn = _invoiceRepository.CreateOpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                var header = MapToHeader(inv, realmId);
                var lineRows = MapToLineUpsertRows(inv, realmId);
                await _invoiceRepository.UpsertInvoicesAsync(new[] { header }, lineRows, conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static QBOInvoiceHeader MapToHeader(QuickBooksInvoiceDto inv, string realmId)
        {
            DateTime txnDate = DateTime.MinValue;
            DateTime dueDate = DateTime.MinValue;

            // Parse TxnDate
            if (!string.IsNullOrWhiteSpace(inv.TxnDate) &&
                DateTime.TryParseExact(
                    inv.TxnDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTxnDate))
            {
                txnDate = parsedTxnDate;
            }

            // Parse DueDate (use TxnDate as fallback if null)
            if (!string.IsNullOrWhiteSpace(inv.DueDate) &&
                DateTime.TryParseExact(
                    inv.DueDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDueDate))
            {
                dueDate = parsedDueDate;
            }
            else
            {
                dueDate = txnDate; // Fallback to TxnDate if DueDate is null
            }

            return new QBOInvoiceHeader
            {
                QBOInvoiceId = inv.QBOId,
                RealmId = realmId,
                SyncToken = inv.SyncToken,
                Domain = inv.Domain,
                Sparse = inv.Sparse,

                TxnDate = txnDate,
                DueDate = dueDate,

                CustomerRefId = inv.CustomerRef?.Value,
                CustomerRefName = inv.CustomerRef?.Name,

                CurrencyCode = inv.CurrencyRef?.Value,
                ExchangeRate = inv.ExchangeRate ?? 1m,

                TotalAmt = inv.TotalAmt,
                Balance = inv.Balance,

                CreateTime = inv.MetaData?.CreateTime ?? DateTimeOffset.UtcNow,
                LastUpdatedTime = inv.MetaData?.LastUpdatedTime ?? DateTimeOffset.UtcNow,

                RawJson = JsonSerializer.Serialize(
                    inv,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })
            };
        }
        /// <summary>Maps QBO invoice DTO to line rows for dbo.InvoiceLineUpsertType (SP).</summary>
        public static List<InvoiceLineUpsertRow> MapToLineUpsertRows(QuickBooksInvoiceDto inv, string realmId)
        {
            var list = new List<InvoiceLineUpsertRow>();
            if (inv?.Line == null || inv.Line.Count == 0)
                return list;

            for (int i = 0; i < inv.Line.Count; i++)
            {
                var line = inv.Line[i];
                var detail = line.SalesItemLineDetail;
                list.Add(new InvoiceLineUpsertRow
                {
                    QBOInvoiceId = inv.QBOId,
                    RealmId = realmId,
                    QBLineId = line.Id,
                    LineNum = line.LineNum ?? i,
                    DetailType = line.DetailType,
                    Description = line.Description,
                    Amount = line.Amount,
                    ItemRefId = detail?.ItemRef?.Value,
                    ItemRefName = detail?.ItemRef?.Name,
                    Qty = detail?.Qty,
                    UnitPrice = detail?.UnitPrice,
                    TaxCodeRef = detail?.TaxCodeRef?.Value,
                    RawLineJson = JsonSerializer.Serialize(
                        line,
                        new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
                });
            }
            return list;
        }

    }
}
