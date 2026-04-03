using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Bills;

public sealed class BillQboCommandService : IBillQboCommandService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksBillService _quickBooksBillService;
    private readonly IBillRepository _billRepository;
    private readonly ILogger<BillQboCommandService> _logger;

    public BillQboCommandService(
        IAuthService authService,
        IQuickBooksBillService quickBooksBillService,
        IBillRepository billRepository,
        ILogger<BillQboCommandService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksBillService = quickBooksBillService ?? throw new ArgumentNullException(nameof(quickBooksBillService));
        _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateBillRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var createResponse = await _quickBooksBillService.CreateBillAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksBillMutationResponse>(createResponse);
            if (parsed?.Bill == null)
                throw new InvalidOperationException("Failed to create bill in QuickBooks or response is invalid.");

            await UpsertSingleBillToDbAsync(parsed.Bill, realmId);
            return ApiResponse<string>.Ok(createResponse, "Bill created successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create bill failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to create bill in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateBillRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var updateResponse = await _quickBooksBillService.UpdateBillAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksBillMutationResponse>(updateResponse);
            if (parsed?.Bill == null)
                throw new InvalidOperationException("Failed to update bill in QuickBooks or response is invalid.");

            await UpsertSingleBillToDbAsync(parsed.Bill, realmId);
            return ApiResponse<string>.Ok(updateResponse, "Bill updated successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update bill failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to update bill in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteBillRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var deleteResponse = await _quickBooksBillService.DeleteBillAsync(token.AccessToken, realmId, jsonPayload);
            await _billRepository.SoftDeleteBillAsync(realmId, request.Id);
            return ApiResponse<string>.Ok(deleteResponse, "Bill deleted successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete bill failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to delete bill in QuickBooks.", new[] { ex.Message });
        }
    }

    private async Task UpsertSingleBillToDbAsync(QuickBooksBillDto bill, string realmId)
    {
        using var conn = _billRepository.CreateOpenConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var header = QuickBooksBillMapper.MapToHeader(bill, realmId);
            var lineRows = QuickBooksBillMapper.MapToLineUpsertRows(bill, realmId);
            await _billRepository.UpsertBillsAsync(new[] { header }, lineRows, conn, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static JsonSerializerOptions SerializerOptions() => new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}
