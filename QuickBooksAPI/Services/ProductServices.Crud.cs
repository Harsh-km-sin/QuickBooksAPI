using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Services;

public partial class ProductServices
{
    public async Task<ApiResponse<string>> CreateProductAsync(CreateProductRequest request)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId);
            var realmId = _requestContext.RealmId;

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
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product creation failed in QBO.", new[] { e.Message });
        }
    }

    public async Task<ApiResponse<string>> UpdateProductAsync(UpdateProductRequest request)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId);
            var realmId = _requestContext.RealmId;

            var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (accessToken == null)
            {
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
            }
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

            var updateResponse = await _quickBooksProductService.UpdateProductAsync(accessToken.AccessToken, realmId, jsonPayload);
            var updatedResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(updateResponse);
            if (updatedResponse?.Item == null)
                throw new Exception("Failed to update product in QBO or response is invalid.");
            var updatedItem = updatedResponse.Item;
            var product = MapDtoToProduct(updatedItem, userId, realmId);
            await _productRepository.UpsertProductsAsync(new List<Products> { product });
            return ApiResponse<string>.Ok(updateResponse, "Product updated successfully in QBO.");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product updation Failed in QBO.", new[] { e.Message });
        }
    }

    public async Task<ApiResponse<string>> DeleteProductAsync(DeleteProductRequest request)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId);
            var realmId = _requestContext.RealmId;

            var accessToken = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (accessToken == null)
            {
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
            }

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
            var deleteResponse = await _quickBooksProductService.DeleteProductAsync(accessToken.AccessToken, realmId, jsonPayload);
            var mutationResponse = JsonSerializer.Deserialize<QuickBooksItemMutationResponse>(deleteResponse);
            if (mutationResponse?.Item == null)
                throw new Exception("Failed to soft delete product in QBO or response is invalid.");

            var product = MapDtoToProduct(mutationResponse.Item, userId, realmId);

            await _productRepository.UpsertProductsAsync(new[] { product });

            return ApiResponse<string>.Ok(deleteResponse, "Product deleted successfully in QBO.");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail("Product deletion failed in QBO.", new[] { e.Message });
        }
    }
}
