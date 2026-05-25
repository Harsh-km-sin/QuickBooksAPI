using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Sync;
using QuickBooksAPI.Features.Products.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services.Sync;
using System.Text.Json;

namespace QuickBooksAPI.Features.Products.Handlers;

public sealed class SyncProductsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;
    private readonly IProductAccountingSyncGateway _productSyncGateway;
    private readonly IProductRepository _productRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;

    public SyncProductsHandler(
        IRequestContext requestContext,
        IAuthService authService,
        IProductAccountingSyncGateway productSyncGateway,
        IProductRepository productRepository,
        IQboSyncStateRepository qboSyncStateRepository)
    {
        _requestContext = requestContext;
        _authService = authService;
        _productSyncGateway = productSyncGateway;
        _productRepository = productRepository;
        _qboSyncStateRepository = qboSyncStateRepository;
    }

    public async Task<ApiResponse<int>> HandleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId!);
            var realmId = _requestContext.RealmId!;

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(
                userId, realmId, QboSyncEntityType.Products);
            var isFirstSync = !lastUpdatedAfter.HasValue;

            lastUpdatedAfter = QboSyncTimeHelper.NormalizeLastUpdatedAfterFromDb(lastUpdatedAfter);

            const int PageSize = 1000;
            var startPosition = 1;
            var totalSyncedCount = 0;
            var hasMore = true;
            DateTime? maxUpdatedTime = null;

            while (hasMore)
            {
                var productsJson = await _productSyncGateway.FetchProductsPageAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter,
                    cancellationToken);

                var productResponse = JsonSerializer.Deserialize<QuickBooksItemQueryResponse>(
                    productsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var products = productResponse?.QueryResponse?.Items?
                    .Select(p => ProductMutationMapper.MapToUpsert(p, userId, realmId))
                    .ToList();

                if (products == null || products.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                foreach (var dto in productResponse!.QueryResponse!.Items!)
                {
                    if (dto.MetaData?.LastUpdatedTime != default)
                    {
                        var dtoLastUpdated = dto.MetaData.LastUpdatedTime;
                        var dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                            ? dtoLastUpdated
                            : dtoLastUpdated.ToUniversalTime();

                        if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            maxUpdatedTime = dtoLastUpdatedUtc;
                    }
                }

                var affectedRows = await _productRepository.UpsertProductsAsync(products);
                totalSyncedCount += affectedRows;

                if (products.Count < PageSize)
                    hasMore = false;
                else
                    startPosition += PageSize;
            }

            if (totalSyncedCount > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = QboSyncTimeHelper.ClampFutureSyncTimestampUtc(maxUpdatedTime.Value);

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboSyncEntityType.Products,
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboSyncEntityType.Products,
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} products.");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.Fail("Failed to sync products.", new[] { ex.Message });
        }
    }
}
