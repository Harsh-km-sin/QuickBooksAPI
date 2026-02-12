using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using System.Text.Json;

namespace QuickBooksAPI.Services
{
    public class ChartOfAccountsServices: IChartOfAccountsService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IQuickBooksChartOfAccountsService _quickBooksChartOfAccountsService;
        private readonly ITokenRepository _tokenRepository;
        private readonly IChartOfAccountsRepository _chartOfAccountsRepository;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;

        public ChartOfAccountsServices(
            ICurrentUser currentUser,
            IQuickBooksChartOfAccountsService quickBooksChartOfAccountsService,
            ITokenRepository tokenRepository,
            IChartOfAccountsRepository chartOfAccountsRepository,
            IQboSyncStateRepository qboSyncStateRepository,
            IAuthService authService)
        {
            _currentUser = currentUser;
            _quickBooksChartOfAccountsService = quickBooksChartOfAccountsService;
            _tokenRepository = tokenRepository;
            _chartOfAccountsRepository = chartOfAccountsRepository;
            _qboSyncStateRepository = qboSyncStateRepository;
            _authService = authService;
        }

        public async Task<ApiResponse<IEnumerable<ChartOfAccounts>>> ListChartOfAccountsAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<ChartOfAccounts>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var accounts = await _chartOfAccountsRepository.GetAllByUserAndRealmAsync(userId, realmId);
            return ApiResponse<IEnumerable<ChartOfAccounts>>.Ok(accounts);
        }

        public async Task<ApiResponse<int>> syncChartOfAccounts()
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Check and refresh token if expired
                var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (accessToken == null)
                {
                    return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Chart_Of_Accounts.ToString());
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
                    var coaJson = await _quickBooksChartOfAccountsService.GetChartOfAccountsAsync(accessToken.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);

                    var root = JsonSerializer.Deserialize<QuickBooksCoaResponse>(coaJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var accounts = root?.QueryResponse?.Account?.Select(a => new ChartOfAccounts
                    {
                        UserId = userId,
                        RealmId = realmId,
                        QBOId = a.Id,
                        Name = a.Name,
                        FullyQualifiedName = a.FullyQualifiedName,
                        SubAccount = a.SubAccount,
                        Active = a.Active,
                        Classification = a.Classification,
                        AccountType = a.AccountType,
                        AccountSubType = a.AccountSubType,
                        CurrentBalance = a.CurrentBalance,
                        CurrentBalanceWithSubAccounts = a.CurrentBalanceWithSubAccounts,
                        CurrencyRefValue = a.CurrencyRef?.Value,
                        CurrencyRefName = a.CurrencyRef?.Name,
                        Domain = a.Domain,
                        Sparse = a.Sparse,
                        SyncToken = a.SyncToken,
                        CreateTime = a.MetaData?.CreateTime ?? DateTime.MinValue,
                        LastUpdatedTime = a.MetaData?.LastUpdatedTime ?? DateTime.MinValue
                    }).ToList();

                    if (accounts == null || accounts.Count == 0)
                    {
                        hasMore = false;
                        continue;
                    }

                    // Track max LastUpdatedTime from synced records
                    // QuickBooks returns timestamps in Pacific Time (-08:00) as ISO 8601 strings
                    // The JSON deserializer parses them, but we need to ensure proper UTC conversion
                    foreach (var dto in root.QueryResponse.Account)
                    {
                        if (dto.MetaData?.LastUpdatedTime != null)
                        {
                            var dtoLastUpdated = dto.MetaData.LastUpdatedTime;
                            
                            // Convert to UTC properly
                            // If Kind is UTC, use as-is; otherwise convert (handles Unspecified/Local)
                            DateTime dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();
                            
                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                maxUpdatedTime = dtoLastUpdatedUtc;
                        }
                    }

                    var affectedRows = await _chartOfAccountsRepository.UpsertChartOfAccountsAsync(accounts);
                    totalSyncedCount += affectedRows;

                    if (accounts.Count < PageSize)
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
                        QboEntityType.Chart_Of_Accounts.ToString(),
                        timeToStore
                    );
                }
                else if (isFirstSync)
                {
                    // First sync with no records - mark that we checked
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Chart_Of_Accounts.ToString(),
                        DateTime.UtcNow
                    );
                }
                // If no records synced and not first sync, don't update sync state (keep previous value)

                return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} accounts.");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail("Failed to sync chart of accounts.", new[] { ex.Message });
            }
        }

    }
}
