using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Services.Vendors;

public sealed class VendorQboCommandService : IVendorQboCommandService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksVendorService _vendorService;
    private readonly IVendorRepository _vendorRepository;
    private readonly ILogger<VendorQboCommandService> _logger;

    public VendorQboCommandService(
        IAuthService authService,
        IQuickBooksVendorService vendorService,
        IVendorRepository vendorRepository,
        ILogger<VendorQboCommandService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _vendorService = vendorService ?? throw new ArgumentNullException(nameof(vendorService));
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateVendorRequest request)
    {
        try
        {
            var errors = VendorRequestValidator.CleanAndValidateForCreate(request);
            if (errors.Count > 0)
                return ApiResponse<string>.Fail("Validation failed.", errors.ToArray());

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var createResponse = await _vendorService.CreateVendorAsync(token.AccessToken, realmId, jsonPayload);
            var createdResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(createResponse);

            if (createdResponse?.Vendor == null)
                throw new InvalidOperationException("Failed to create vendor in QBO or response is invalid.");

            var vendor = QuickBooksVendorMapper.Map(createdResponse.Vendor, userId, realmId);
            await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);
            return ApiResponse<string>.Ok(createResponse, "Vendor created successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create vendor failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to create vendor in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> SoftDeleteAsync(int userId, string realmId, SoftDeleteVendorRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var deleteResponse = await _vendorService.SoftDeleteVendorAsync(token.AccessToken, realmId, request.Id, request.SyncToken);
            var deletedResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(deleteResponse);

            if (deletedResponse?.Vendor == null)
                return ApiResponse<string>.Fail("Failed to delete vendor in QuickBooks. The vendor may already be inactive or invalid Id/SyncToken.");

            var vendor = QuickBooksVendorMapper.Map(deletedResponse.Vendor, userId, realmId);
            await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);

            return ApiResponse<string>.Ok(deleteResponse, "Vendor deleted successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soft delete vendor failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to delete vendor in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateVendorRequest request)
    {
        try
        {
            var errors = VendorRequestValidator.CleanAndValidateForUpdate(request);
            if (errors.Count > 0)
                return ApiResponse<string>.Fail("Validation failed.", errors.ToArray());

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var updateResponse = await _vendorService.UpdateVendorAsync(token.AccessToken, realmId, jsonPayload);
            var updatedResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(updateResponse);

            if (updatedResponse?.Vendor == null)
                throw new InvalidOperationException("Failed to update vendor in QBO or response is invalid.");

            var vendor = QuickBooksVendorMapper.Map(updatedResponse.Vendor, userId, realmId);

            await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);
            return ApiResponse<string>.Ok(updateResponse, "Vendor updated successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update vendor failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to update vendor in QuickBooks.", new[] { ex.Message });
        }
    }
}
