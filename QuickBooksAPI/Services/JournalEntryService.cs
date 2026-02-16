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
    public class JournalEntryService : IJournalEntryService
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITokenRepository _tokenRepository;
        private readonly IQuickBooksJournalEntryService _quickBooksJournalEntryService;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;

        public JournalEntryService(
            ICurrentUser currentUser,
            ITokenRepository tokenRepository,
            IQuickBooksJournalEntryService quickBooksJournalEntryService,
            IJournalEntryRepository journalEntryRepository,
            IQboSyncStateRepository qboSyncStateRepository,
            IAuthService authService)
        {
            _currentUser = currentUser;
            _tokenRepository = tokenRepository;
            _quickBooksJournalEntryService = quickBooksJournalEntryService;
            _journalEntryRepository = journalEntryRepository;
            _qboSyncStateRepository = qboSyncStateRepository;
            _authService = authService;
        }

        //public async Task<ApiResponse<IEnumerable<QBOJournalEntryHeader>>> ListJournalEntriesAsync()
        //{
        //    if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
        //        return ApiResponse<IEnumerable<QBOJournalEntryHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        //    var realmId = _currentUser.RealmId;
        //    var entries = await _journalEntryRepository.GetAllByRealmAsync(realmId);
        //    return ApiResponse<IEnumerable<QBOJournalEntryHeader>>.Ok(entries);
        //}

        public async Task<ApiResponse<PagedResult<QBOJournalEntryHeader>>> ListJournalEntriesAsync(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var realmId = _currentUser.RealmId;
            var page = query.GetPage();
            var pageSize = query.GetPageSize();
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var result = await _journalEntryRepository.GetPagedByRealmAsync(realmId, page, pageSize, search);
            return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Ok(result);
        }

        public async Task<ApiResponse<int>> SyncJournalEntriesAsync()
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Check and refresh token if expired
                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                {
                    return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Manual_Journals.ToString());
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

                const int PageSize = 1000;
                int startPosition = 1;
                int totalSyncedCount = 0;
                bool hasMore = true;
                DateTime? maxUpdatedTime = null; // Track max from actual synced records only

                while (hasMore)
                {
                    var journalEntriesJson = await _quickBooksJournalEntryService.GetJournalEntryAsync(token.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);
                    var journalEntryResponse = JsonSerializer.Deserialize<QuickBooksJournalEntryResponse>(journalEntriesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Console.WriteLine("Raw Response: journalEntriesJson" + journalEntriesJson);
                    Console.WriteLine("Raw response: journalEntryResponse" + journalEntryResponse);
                    var journalEntries = journalEntryResponse?.QueryResponse?.JournalEntry;
                    if (journalEntries == null || journalEntries.Count == 0)
                    {
                        hasMore = false;
                        continue;
                    }

                    using var conn = _journalEntryRepository.CreateOpenConnection();
                    using var tx = conn.BeginTransaction();
                    try
                    {
                        foreach (var je in journalEntries)
                        {
                            // Track max LastUpdatedTime from synced records
                            // QuickBooks returns timestamps in Pacific Time (-08:00) as ISO 8601 strings
                            // The JSON deserializer parses them, but we need to ensure proper UTC conversion
                            if (je.MetaData?.LastUpdatedTime != null)
                            {
                                var dtoLastUpdated = je.MetaData.LastUpdatedTime.Value;
                                var dtoLastUpdatedUtc = dtoLastUpdated.UtcDateTime;

                                if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                    maxUpdatedTime = dtoLastUpdatedUtc;
                            }

                            // 1️⃣ Upsert header
                            var header = MapToHeader(je, realmId);
                            await _journalEntryRepository.UpsertJournalEntryHeadersAsync(new[] { header }, conn, tx);

                            // 2️⃣ Fetch DB PK
                            var journalEntryId = await _journalEntryRepository.GetJournalEntryIdAsync(je.Id, realmId, conn, tx);

                            // 3️⃣ Replace lines
                            await _journalEntryRepository.DeleteJournalEntryLinesAsync(journalEntryId, conn, tx);

                            var lines = MapToLines(je, journalEntryId);
                            await _journalEntryRepository.InsertJournalEntryLinesAsync(lines, conn, tx);

                            totalSyncedCount++;
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }

                    if (journalEntries.Count < PageSize)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        startPosition += PageSize;
                    }
                }

                // Update sync state after successful sync
                if (totalSyncedCount > 0 && maxUpdatedTime.HasValue)
                {
                    // Ensure we don't store a time in the future (safeguard against timezone issues)
                    var timeToStore = maxUpdatedTime.Value;
                    var nowUtc = DateTime.UtcNow;
                    if (timeToStore > nowUtc.AddSeconds(30))
                    {
                        timeToStore = nowUtc;
                    }
                    
                    // We synced records, use the max LastUpdatedTime from those records (already UTC)
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Manual_Journals.ToString(),
                        timeToStore
                    );
                }
                else if (isFirstSync)
                {
                    // First sync with no records - mark that we checked
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Manual_Journals.ToString(),
                        DateTime.UtcNow
                    );
                }
                // If no records synced and not first sync, don't update sync state (keep previous value)

                return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} journal entries.");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail("Failed to sync journal entries.", new[] { ex.Message });
            }
        }
        public static QBOJournalEntryHeader MapToHeader(JournalEntry je, string realmId)
        {
            DateTime? txnDate = null;

            if (!string.IsNullOrWhiteSpace(je.TxnDate) &&
                DateTime.TryParseExact(
                    je.TxnDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                txnDate = parsedDate;
            }

            return new QBOJournalEntryHeader
            {
                QBJournalEntryId = je.Id,
                QBRealmId = realmId,
                SyncToken = je.SyncToken,
                Domain = je.Domain,

                TxnDate = txnDate,

                Sparse = je.Sparse,
                Adjustment = je.Adjustment,

                DocNumber = je.DocNumber,
                PrivateNote = je.PrivateNote,

                CurrencyCode = je.CurrencyRef?.Value,
                ExchangeRate = je.ExchangeRate,

                TotalAmount = je.TotalAmt,
                HomeTotalAmount = je.HomeTotalAmt,

                CreateTime = je.MetaData?.CreateTime,
                LastUpdatedTime = je.MetaData?.LastUpdatedTime,

                RawJson = JsonSerializer.Serialize(
                    je,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })
            };
        }
        public static IEnumerable<QBOJournalEntryLine> MapToLines(JournalEntry je,long journalEntryId)
        {
            if (je?.Line == null || je.Line.Count == 0)
                yield break;

            for (int i = 0; i < je.Line.Count; i++)
            {
                var line = je.Line[i];
                var detail = line.JournalEntryLineDetail;

                yield return new QBOJournalEntryLine
                {
                    JournalEntryId = journalEntryId,
                    QBLineId = line.Id,
                    LineNum = i,

                    DetailType = line.DetailType,
                    Description = line.Description,

                    Amount = line.Amount,

                    PostingType = detail?.PostingType,

                    AccountRefId = detail?.AccountRef?.Value,
                    AccountRefName = detail?.AccountRef?.Name,

                    EntityType = detail?.Entity?.Type,
                    EntityRefId = detail?.Entity?.Ref?.Value,
                    EntityRefName = detail?.Entity?.Ref?.Name,

                    RawLineJson = JsonSerializer.Serialize(
                        line,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        })
                };
            }
        }
    }
}
