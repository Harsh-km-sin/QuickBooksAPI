using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Features.Products.Handlers;

public sealed class DeleteProductHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;
    private readonly IProductAccountingCommandGateway _productCommands;
    private readonly IProductRepository _productRepository;

    public DeleteProductHandler(
        IRequestContext requestContext,
        IAuthService authService,
        IProductAccountingCommandGateway productCommands,
        IProductRepository productRepository)
    {
        _requestContext = requestContext;
        _authService = authService;
        _productCommands = productCommands;
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<string>> HandleAsync(DeleteProductRequest request)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId!);
            var realmId = _requestContext.RealmId!;

            var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (accessToken == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.SyncToken))
            {
                return ApiResponse<string>.Fail(
                    "Id and SyncToken are required to delete a product in QBO.");
            }

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });
            var deleteResponse = await _productCommands.DeleteProductAsync(accessToken.AccessToken, realmId, jsonPayload);
            var mutationResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(deleteResponse);
            if (mutationResponse?.Item == null)
                throw new Exception("Failed to soft delete product in QBO or response is invalid.");

            var product = ProductMutationMapper.MapToUpsert(mutationResponse.Item, userId, realmId);

            await _productRepository.UpsertProductsAsync(new[] { product });

            return ApiResponse<string>.Ok(deleteResponse, "Product deleted successfully in QBO.");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product deletion failed in QBO.", new[] { e.Message });
        }
    }
}
