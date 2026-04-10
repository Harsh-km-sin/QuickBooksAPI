using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services.Sync;
using System.Text.Json;

namespace QuickBooksAPI.Services;

public partial class ProductServices
{
    public async Task<ApiResponse<int>> GetProductsAsync()
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId);
            var realmId = _requestContext.RealmId;

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
            {
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");
            }

            var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Products.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            lastUpdatedAfter = QboSyncTimeHelper.NormalizeLastUpdatedAfterFromDb(lastUpdatedAfter);

            const int PageSize = 1000;
            int startPosition = 1;
            int totalSyncedCount = 0;
            bool hasMore = true;
            DateTime? maxUpdatedTime = null;

            while (hasMore)
            {
                var productsJson = await _productSyncGateway.FetchProductsPageAsync(token.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);
                var productResponse = JsonSerializer.Deserialize<QuickBooksItemQueryResponse>(productsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var products = productResponse?.QueryResponse?.Items?.Select(p => MapDtoToProduct(p, userId, realmId)).ToList();

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
                        DateTime dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                            ? dtoLastUpdated
                            : dtoLastUpdated.ToUniversalTime();

                        if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            maxUpdatedTime = dtoLastUpdatedUtc;
                    }
                }

                var affectedRows = await _productRepository.UpsertProductsAsync(products);
                totalSyncedCount += affectedRows;

                if (products.Count < PageSize)
                {
                    hasMore = false;
                }
                else
                {
                    startPosition += PageSize;
                }
            }

            if (totalSyncedCount > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = QboSyncTimeHelper.ClampFutureSyncTimestampUtc(maxUpdatedTime.Value);

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Products.ToString(),
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Products.ToString(),
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
