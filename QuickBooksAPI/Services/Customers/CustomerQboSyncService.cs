using System.Text.Json;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Customers;

public sealed class CustomerQboSyncService : ICustomerQboSyncService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksCustomerService _quickBooksCustomerService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly ILogger<CustomerQboSyncService> _logger;

    public CustomerQboSyncService(
        IAuthService authService,
        IQuickBooksCustomerService quickBooksCustomerService,
        ICustomerRepository customerRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        ILogger<CustomerQboSyncService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksCustomerService = quickBooksCustomerService ?? throw new ArgumentNullException(nameof(quickBooksCustomerService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _qboSyncStateRepository = qboSyncStateRepository ?? throw new ArgumentNullException(nameof(qboSyncStateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Customer.ToString());
            _logger.LogInformation("[CustomerSync] Started sync for user {UserId}. LastUpdatedAfter from DB: {LastUpdatedAfter}", userId, lastUpdatedAfter);

            var isFirstSync = !lastUpdatedAfter.HasValue;
            var originalLastUpdatedAfter = lastUpdatedAfter;

            if (lastUpdatedAfter.HasValue && lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);

            const int PageSize = 1000;
            var startPosition = 1;
            var totalSynced = 0;
            var hasMore = true;
            DateTime? maxUpdatedTime = null;

            while (hasMore)
            {
                var customersJson = await _quickBooksCustomerService.GetCustomersAsync(token.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);
                var customerResponse = JsonSerializer.Deserialize<QuickBooksCustomerQueryResponse>(customersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var customerCount = customerResponse?.QueryResponse?.Customers?.Count ?? 0;
                _logger.LogInformation("[CustomerSync] Fetched page starting at {Start}. Count: {Count}", startPosition, customerCount);

                var customers = customerResponse?.QueryResponse?.Customers?.Select(c => QuickBooksCustomerMapper.Map(c, userId, realmId)).ToList();

                if (customers != null)
                {
                    foreach (var c in customers)
                        _logger.LogInformation("[CustomerSync] Processing customer. QboId={QboId}, LastUpdatedTime={LastUpdatedTime:O}", c.QboId, c.LastUpdatedTime);
                }

                if (customers == null || customers.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                if (customerResponse?.QueryResponse?.Customers != null)
                {
                    foreach (var dto in customerResponse.QueryResponse.Customers)
                    {
                        if (dto.MetaData?.LastUpdatedTime != default)
                        {
                            var dtoLastUpdated = dto.MetaData.LastUpdatedTime;
                            var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();

                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            {
                                maxUpdatedTime = dtoLastUpdatedUtc;
                                _logger.LogInformation("[CustomerSync] Tracking MaxLastUpdatedTime from DTO: {Utc:O} (original: {Orig:O}, Kind: {Kind})", dtoLastUpdatedUtc, dtoLastUpdated, dtoLastUpdated.Kind);
                            }
                        }
                    }
                }

                await _customerRepository.UpsertCustomersAsync(customers, userId, realmId);
                totalSynced += customers.Count;

                if (customers.Count < PageSize)
                    hasMore = false;
                else
                    startPosition += PageSize;
            }

            if (totalSynced > 0)
            {
                DateTime timeToStore;

                if (maxUpdatedTime.HasValue)
                {
                    timeToStore = maxUpdatedTime.Value;
                    var nowUtc = DateTime.UtcNow;
                    if (timeToStore > nowUtc.AddSeconds(30))
                        timeToStore = nowUtc;
                }
                else if (originalLastUpdatedAfter.HasValue)
                    timeToStore = originalLastUpdatedAfter.Value;
                else
                    timeToStore = DateTime.UtcNow;

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Customer.ToString(),
                    timeToStore);
                _logger.LogInformation("[CustomerSync] Updated SyncState with MaxLastUpdatedTime from records: {Time:O}", timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Customer.ToString(),
                    DateTime.UtcNow);
            }

            if (totalSynced == 0 && !isFirstSync)
                _logger.LogInformation("[CustomerSync] Completed with 0 customers synced. SyncState remains unchanged (LastUpdatedAfter: {Last}).", originalLastUpdatedAfter);

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} customers.");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.Fail("Failed to sync customers.", new[] { ex.Message });
        }
    }
}
