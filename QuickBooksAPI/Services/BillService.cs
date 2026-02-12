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

namespace QuickBooksAPI.Services
{
    public class BillService : IBillService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IQuickBooksBillService _quickBooksBillService;
        private readonly IBillRepository _billRepository;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;

        public BillService(
            ICurrentUser currentUser,
            IQuickBooksBillService quickBooksBillService,
            IBillRepository billRepository,
            IQboSyncStateRepository qboSyncStateRepository,
            IAuthService authService)
        {
            _currentUser = currentUser;
            _quickBooksBillService = quickBooksBillService;
            _billRepository = billRepository;
            _qboSyncStateRepository = qboSyncStateRepository;
            _authService = authService;
        }

        public async Task<ApiResponse<IEnumerable<QBOBillHeader>>> ListBillsAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<QBOBillHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var realmId = _currentUser.RealmId;
            var bills = await _billRepository.GetAllByRealmAsync(realmId);
            return ApiResponse<IEnumerable<QBOBillHeader>>.Ok(bills);
        }

        public async Task<ApiResponse<int>> SyncBillsAsync()
        {
            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            const int PageSize = 1000;
            int startPosition = 1;
            int totalSynced = 0;

            var lastUpdatedAfter = await _qboSyncStateRepository
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Bills.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            if (lastUpdatedAfter.HasValue)
            {
                if (lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                    lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);
            }

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

                        pageHeaders.Add(MapToHeader(bill, realmId));
                        pageLines.AddRange(MapToLineUpsertRows(bill, realmId));
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

        public async Task<ApiResponse<string>> CreateBillAsync(CreateBillRequest request)
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

                var createResponse = await _quickBooksBillService.CreateBillAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksBillMutationResponse>(createResponse);
                if (parsed?.Bill == null)
                    throw new InvalidOperationException("Failed to create bill in QuickBooks or response is invalid.");

                await UpsertSingleBillToDbAsync(parsed.Bill, realmId);
                return ApiResponse<string>.Ok(createResponse, "Bill created successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to create bill in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> UpdateBillAsync(UpdateBillRequest request)
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

                var updateResponse = await _quickBooksBillService.UpdateBillAsync(token.AccessToken, realmId, jsonPayload);
                var parsed = JsonSerializer.Deserialize<QuickBooksBillMutationResponse>(updateResponse);
                if (parsed?.Bill == null)
                    throw new InvalidOperationException("Failed to update bill in QuickBooks or response is invalid.");

                await UpsertSingleBillToDbAsync(parsed.Bill, realmId);
                return ApiResponse<string>.Ok(updateResponse, "Bill updated successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to update bill in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> DeleteBillAsync(DeleteBillRequest request)
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

                var deleteResponse = await _quickBooksBillService.DeleteBillAsync(token.AccessToken, realmId, jsonPayload);
                await _billRepository.SoftDeleteBillAsync(realmId, request.Id);
                return ApiResponse<string>.Ok(deleteResponse, "Bill deleted successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to delete bill in QuickBooks.", new[] { ex.Message });
            }
        }

        private async Task UpsertSingleBillToDbAsync(QuickBooksBillDto bill, string realmId)
        {
            using var conn = _billRepository.CreateOpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                var header = MapToHeader(bill, realmId);
                var lineRows = MapToLineUpsertRows(bill, realmId);
                await _billRepository.UpsertBillsAsync(new[] { header }, lineRows, conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static QBOBillHeader MapToHeader(QuickBooksBillDto bill, string realmId)
        {
            DateTime? txnDate = null;
            DateTime? dueDate = null;

            if (!string.IsNullOrWhiteSpace(bill.TxnDate) &&
                DateTime.TryParseExact(bill.TxnDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTxn))
                txnDate = parsedTxn;
            if (!string.IsNullOrWhiteSpace(bill.DueDate) &&
                DateTime.TryParseExact(bill.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDue))
                dueDate = parsedDue;
            else if (txnDate.HasValue)
                dueDate = txnDate;

            return new QBOBillHeader
            {
                QBOBillId = bill.QBOId,
                RealmId = realmId,
                SyncToken = bill.SyncToken ?? "",
                Domain = bill.Domain,
                Sparse = bill.Sparse,
                APAccountRefValue = bill.APAccountRef?.Value,
                APAccountRefName = bill.APAccountRef?.Name,
                VendorRefValue = bill.VendorRef?.Value,
                VendorRefName = bill.VendorRef?.Name,
                TxnDate = txnDate,
                DueDate = dueDate,
                TotalAmt = bill.TotalAmt,
                Balance = bill.Balance,
                CurrencyRefValue = bill.CurrencyRef?.Value,
                CurrencyRefName = bill.CurrencyRef?.Name,
                SalesTermRefValue = bill.SalesTermRef?.Value,
                CreateTime = bill.MetaData != null ? new DateTimeOffset(bill.MetaData.CreateTime.ToUniversalTime()) : DateTimeOffset.UtcNow,
                LastUpdatedTime = bill.MetaData != null ? new DateTimeOffset(bill.MetaData.LastUpdatedTime.ToUniversalTime()) : DateTimeOffset.UtcNow,
                RawJson = JsonSerializer.Serialize(bill, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
            };
        }

        private static List<BillLineUpsertRow> MapToLineUpsertRows(QuickBooksBillDto bill, string realmId)
        {
            var list = new List<BillLineUpsertRow>();
            if (bill.Line == null || bill.Line.Count == 0)
                return list;

            for (var i = 0; i < bill.Line.Count; i++)
            {
                var line = bill.Line[i];
                var accDetail = line.AccountBasedExpenseLineDetail;
                var itemDetail = line.ItemBasedExpenseLineDetail;
                list.Add(new BillLineUpsertRow
                {
                    QBOBillId = bill.QBOId,
                    RealmId = realmId,
                    QBLineId = line.Id,
                    LineNum = i,
                    DetailType = line.DetailType,
                    Description = line.Description,
                    Amount = line.Amount,
                    ProjectRefValue = line.ProjectRef?.Value,
                    AccountRefValue = accDetail?.AccountRef?.Value,
                    AccountRefName = accDetail?.AccountRef?.Name,
                    TaxCodeRefValue = accDetail?.TaxCodeRef?.Value ?? itemDetail?.TaxCodeRef?.Value,
                    BillableStatus = accDetail?.BillableStatus ?? itemDetail?.BillableStatus,
                    CustomerRefValue = accDetail?.CustomerRef?.Value,
                    CustomerRefName = accDetail?.CustomerRef?.Name,
                    ItemRefValue = itemDetail?.ItemRef?.Value,
                    ItemRefName = itemDetail?.ItemRef?.Name,
                    Qty = itemDetail?.Qty,
                    UnitPrice = itemDetail?.UnitPrice,
                    RawLineJson = JsonSerializer.Serialize(line, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
                });
            }
            return list;
        }
    }
}
