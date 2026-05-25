using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Features.Products.Handlers;

public sealed class CreateProductHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;
    private readonly IProductAccountingCommandGateway _productCommands;
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(
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

    public async Task<ApiResponse<string>> HandleAsync(CreateProductRequest request)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId!);
            var realmId = _requestContext.RealmId!;

            var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (accessToken == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var createResponse = await _productCommands.CreateProductAsync(accessToken.AccessToken, realmId, jsonPayload);

            var createdResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(createResponse);

            if (createdResponse?.Item == null)
                throw new Exception("Failed to create product in QBO or response is invalid.");

            var createdItem = createdResponse.Item;
            var product = ProductMutationMapper.MapToUpsert(createdItem, userId, realmId);
            await _productRepository.UpsertProductsAsync(new[] { product });
            return ApiResponse<string>.Ok(createResponse, "Product created successfully in QBO.");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product creation failed in QBO.", new[] { e.Message });
        }
    }
}
