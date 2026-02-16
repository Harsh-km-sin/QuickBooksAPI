using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksService.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Services
{
    public class ProductServices : IProductService
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITokenRepository _tokenRepository;
        private readonly IQuickBooksProductService _quickBooksProductService;
        private readonly IProductRepository _productRepository;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;

        public ProductServices(
            ICurrentUser currentUser,
            ITokenRepository tokenRepository,
            IQuickBooksProductService quickBooksProductService,
            IProductRepository productRepository,
            IQboSyncStateRepository qboSyncStateRepository,
            IAuthService authService)
        {
            _currentUser = currentUser;
            _tokenRepository = tokenRepository;
            _quickBooksProductService = quickBooksProductService;
            _productRepository = productRepository;
            _qboSyncStateRepository = qboSyncStateRepository;
            _authService = authService;
        }
        public async Task<ApiResponse<IEnumerable<Products>>> ListProductsAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<Products>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var products = await _productRepository.GetAllByUserAndRealmAsync(userId, realmId);
            return ApiResponse<IEnumerable<Products>>.Ok(products);
        }

        public async Task<ApiResponse<PagedResult<Products>>> ListProductsAsync(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<PagedResult<Products>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var page = query.GetPage();
            var pageSize = query.GetPageSize();
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var result = await _productRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search);
            return ApiResponse<PagedResult<Products>>.Ok(result);
        }

        public async Task<ApiResponse<int>> GetProductsAsync()
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

                var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Products.ToString());
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
                    var productsJson = await _quickBooksProductService.GetProductsAsync(token.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);
                    var productResponse = JsonSerializer.Deserialize<QuickBooksItemQueryResponse>(productsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var products = productResponse?.QueryResponse?.Items?.Select(p => MapDtoToProduct(p, userId, realmId)).ToList();

                    if (products == null || products.Count == 0)
                    {
                        hasMore = false;
                        continue;
                    }

                    // Track max LastUpdatedTime from synced records
                    // QuickBooks returns timestamps in Pacific Time (-08:00) as ISO 8601 strings
                    // The JSON deserializer parses them, but we need to ensure proper UTC conversion
                    foreach (var dto in productResponse.QueryResponse.Items)
                    {
                        if (dto.MetaData?.LastUpdatedTime != default)
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
                        QboEntityType.Products.ToString(),
                        timeToStore
                    );
                }
                else if (isFirstSync)
                {
                    // First sync with no records - mark that we checked
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Products.ToString(),
                        DateTime.UtcNow
                    );
                }
                // If no records synced and not first sync, don't update sync state (keep previous value)

                return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} products.");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail("Failed to sync products.", new[] { ex.Message });
            }
        }
        public async Task<ApiResponse<string>> CreateProductAsync(CreateProductRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;
                
                // Check and refresh token if expired
                var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (accessToken == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var createResponse = await _quickBooksProductService.CreateProductAsync(accessToken.AccessToken, realmId, jsonPayload);

                var createdResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(createResponse);

                if (createdResponse?.Item == null)
                    throw new Exception("Failed to create product in QBO or response is invalid.");

                var createdItem = createdResponse.Item;
                var product = MapDtoToProduct(createdItem, userId, realmId);
                await _productRepository.UpsertProductsAsync(new List<Products> { product });
                return ApiResponse<string>.Ok(createResponse, "Product created successfully in QBO.");
            }catch(Exception e)
            {
                return ApiResponse<string>.Fail("Product creation failed in QBO.", new[] { e.Message });
            }
        }
        public async Task<ApiResponse<string>> UpdateProductAsync(UpdateProductRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;
                
                // Check and refresh token if expired
                var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (accessToken == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }
                if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.SyncToken))
                {
                    return ApiResponse<string>.Fail(
                        "Id and SyncToken are required to update a product in QBO."
                    );
                }
                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });
           
                var updateResponse = await _quickBooksProductService.UpdateProductAsync(accessToken.AccessToken, realmId, jsonPayload);
                var updatedResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(updateResponse);
                if (updatedResponse?.Item == null)
                    throw new Exception("Failed to update product in QBO or response is invalid.");
                var updatedItem = updatedResponse.Item;
                var product = MapDtoToProduct(updatedItem, userId, realmId);
                await _productRepository.UpsertProductsAsync(new List<Products> { product });
                return ApiResponse<string>.Ok(updateResponse, "Product updated successfully in QBO.");
            }catch(Exception e)
            {
                return ApiResponse<string>.Fail("Product updation Failed in QBO.", new[] { e.Message });
            }
        }
        public async Task<ApiResponse<string>> DeleteProductAsync(DeleteProductRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;
                
                // Check and refresh token if expired
                var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (accessToken == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.SyncToken))
                {
                    return ApiResponse<string>.Fail(
                        "Id and SyncToken are required to delete a product in QBO."
                    );
                }

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });
                var deleteResponse = await _quickBooksProductService.DeleteProductAsync(accessToken.AccessToken, realmId, jsonPayload);
                var mutationResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(deleteResponse);
                if (mutationResponse?.Item == null)
                    throw new Exception("Failed to soft delete product in QBO or response is invalid.");

                var product = MapDtoToProduct(mutationResponse.Item,userId,realmId);

                await _productRepository.UpsertProductsAsync(new[] { product });

                return ApiResponse<string>.Ok(deleteResponse, "Product deleted successfully in QBO.");
            }
            catch (Exception e)
            {
                return ApiResponse<string>.Fail("Product deletion failed in QBO.", new[] { e.Message });
            }
        }
        private Products MapDtoToProduct(QuickBooksItemDto dto, int userId, string realmId)
        {
            return new Products
            {
                QBOId = dto.QBOId,
                Name = dto.Name,
                Description = dto.Description,
                Active = dto.Active,
                FullyQualifiedName = dto.FullyQualifiedName,
                Taxable = dto.Taxable,
                UnitPrice = dto.UnitPrice,
                Type = dto.Type,
                QtyOnHand = dto.QtyOnHand ?? 0, // default to 0 if null
                IncomeAccountRefValue = dto.IncomeAccountRef?.Value,
                IncomeAccountRefName = dto.IncomeAccountRef?.Name,
                PurchaseCost = dto.PurchaseCost,
                TrackQtyOnHand = dto.TrackQtyOnHand,
                Domain = dto.Domain,
                Sparse = dto.Sparse,
                SyncToken = dto.SyncToken,
                CreateTime = dto.MetaData?.CreateTime ?? DateTime.Now,
                LastUpdatedTime = dto.MetaData?.LastUpdatedTime ?? DateTime.Now,
                UserId = userId,
                RealmId = realmId
            };
        }

    }
}
