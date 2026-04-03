using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Customers;

public sealed class CustomerQboCommandService : ICustomerQboCommandService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksCustomerService _quickBooksCustomerService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerQboCommandService> _logger;

    public CustomerQboCommandService(
        IAuthService authService,
        IQuickBooksCustomerService quickBooksCustomerService,
        ICustomerRepository customerRepository,
        ILogger<CustomerQboCommandService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksCustomerService = quickBooksCustomerService ?? throw new ArgumentNullException(nameof(quickBooksCustomerService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateCustomerRequest request)
    {
        try
        {
            var validationErrors = CustomerRequestValidator.CleanAndValidateForCreate(request);
            if (validationErrors.Count > 0)
                return ApiResponse<string>.Fail("Validation failed.", validationErrors);

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var createResponse = await _quickBooksCustomerService.CreateCustomerAsync(token.AccessToken, realmId, jsonPayload);
            var createdResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(createResponse);

            if (createdResponse?.Customer == null)
                throw new InvalidOperationException("Failed to create customer in QBO or response is invalid.");

            var customer = QuickBooksCustomerMapper.Map(createdResponse.Customer, userId, realmId);
            await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
            return ApiResponse<string>.Ok(createResponse, "Customer created successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create customer failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to create customer in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateCustomerRequest request)
    {
        try
        {
            var validationErrors = CustomerRequestValidator.CleanAndValidateForUpdate(request);
            if (validationErrors.Count > 0)
                return ApiResponse<string>.Fail("Validation failed.", validationErrors);

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var updateResponse = await _quickBooksCustomerService.UpdateCustomerAsync(token.AccessToken, realmId, jsonPayload);
            var updatedResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(updateResponse);
            if (updatedResponse?.Customer == null)
                throw new InvalidOperationException("Failed to update customer in QBO or response is invalid.");

            var customer = QuickBooksCustomerMapper.Map(updatedResponse.Customer, userId, realmId);
            await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
            return ApiResponse<string>.Ok(updateResponse, "Customer updated successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update customer failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to update customer in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteCustomerRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            var deleteResponse = await _quickBooksCustomerService.DeleteCustomerAsync(token.AccessToken, realmId, jsonPayload);
            var deletedResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(deleteResponse);
            if (deletedResponse?.Customer == null)
                throw new InvalidOperationException("Failed to delete customer in QBO or response is invalid.");

            var customer = QuickBooksCustomerMapper.Map(deletedResponse.Customer, userId, realmId);
            await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
            return ApiResponse<string>.Ok(deleteResponse, "Customer deleted successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete customer failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to delete customer in QuickBooks.", new[] { ex.Message });
        }
    }
}
