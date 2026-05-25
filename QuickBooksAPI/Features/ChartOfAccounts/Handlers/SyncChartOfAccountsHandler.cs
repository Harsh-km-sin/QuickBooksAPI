using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Sync;
using QuickBooksAPI.Features.ChartOfAccounts.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using System.Text.Json;

namespace QuickBooksAPI.Features.ChartOfAccounts.Handlers;

public sealed class SyncChartOfAccountsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;
    private readonly IChartOfAccountsAccountingGateway _coaGateway;
    private readonly IChartOfAccountsRepository _chartOfAccountsRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;

    public SyncChartOfAccountsHandler(
        IRequestContext requestContext,
        IAuthService authService,
        IChartOfAccountsAccountingGateway coaGateway,
        IChartOfAccountsRepository chartOfAccountsRepository,
        IQboSyncStateRepository qboSyncStateRepository)
    {
        _requestContext = requestContext;
        _authService = authService;
        _coaGateway = coaGateway;
        _chartOfAccountsRepository = chartOfAccountsRepository;
        _qboSyncStateRepository = qboSyncStateRepository;
    }

    public async Task<ApiResponse<int>> HandleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId!);
            var realmId = _requestContext.RealmId!;

            var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (accessToken == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(
                userId, realmId, QboSyncEntityType.ChartOfAccounts);
            var isFirstSync = !lastUpdatedAfter.HasValue;

            if (lastUpdatedAfter.HasValue)
            {
                if (lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                    lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);
            }

            const int PageSize = 1000;
            var startPosition = 1;
            var totalSyncedCount = 0;
            var hasMore = true;
            DateTime? maxUpdatedTime = null;

            while (hasMore)
            {
                var coaJson = await _coaGateway.FetchAccountsPageAsync(
                    accessToken.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter,
                    cancellationToken);

                var root = JsonSerializer.Deserialize<QuickBooksCoaResponse>(coaJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var accounts = root?.QueryResponse?.Account?
                    .Select(a => ChartOfAccountsMutationMapper.MapToUpsert(a, userId, realmId))
                    .ToList();

                if (accounts == null || accounts.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                foreach (var dto in root!.QueryResponse!.Account!)
                {
                    if (dto.MetaData?.LastUpdatedTime != null)
                    {
                        var dtoLastUpdated = dto.MetaData.LastUpdatedTime;
                        var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                            ? dtoLastUpdated
                            : dtoLastUpdated.ToUniversalTime();

                        if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            maxUpdatedTime = dtoLastUpdatedUtc;
                    }
                }

                var affectedRows = await _chartOfAccountsRepository.UpsertChartOfAccountsAsync(accounts);
                totalSyncedCount += affectedRows;

                if (accounts.Count < PageSize)
                    hasMore = false;
                else
                    startPosition += PageSize;
            }

            if (totalSyncedCount > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = maxUpdatedTime.Value;
                var nowUtc = DateTime.UtcNow;
                if (timeToStore > nowUtc.AddSeconds(30))
                    timeToStore = nowUtc;

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboSyncEntityType.ChartOfAccounts,
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboSyncEntityType.ChartOfAccounts,
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} accounts.");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.Fail("Failed to sync chart of accounts.", new[] { ex.Message });
        }
    }
}
