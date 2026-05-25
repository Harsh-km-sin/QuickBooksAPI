using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Features.Products.Handlers;

public sealed class UpdateProductHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;
    private readonly IProductAccountingCommandGateway _productCommands;
    private readonly IProductRepository _productRepository;

    public UpdateProductHandler(
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

    public async Task<ApiResponse<string>> HandleAsync(UpdateProductRequest request)
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
                    "Id and SyncToken are required to update a product in QBO.");
            }

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var updateResponse = await _productCommands.UpdateProductAsync(accessToken.AccessToken, realmId, jsonPayload);
            var updatedResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(updateResponse);
            if (updatedResponse?.Item == null)
                throw new Exception("Failed to update product in QBO or response is invalid.");
            var updatedItem = updatedResponse.Item;
            var product = ProductMutationMapper.MapToUpsert(updatedItem, userId, realmId);
            await _productRepository.UpsertProductsAsync(new[] { product });
            return ApiResponse<string>.Ok(updateResponse, "Product updated successfully in QBO.");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product updation Failed in QBO.", new[] { e.Message });
        }
    }
}
