using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services.Invoices;

public sealed class InvoiceQboCommandService : IInvoiceQboCommandService
{
    private readonly IAuthService _authService;
    private readonly IQuickBooksInvoiceService _quickBooksInvoiceService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<InvoiceQboCommandService> _logger;

    public InvoiceQboCommandService(
        IAuthService authService,
        IQuickBooksInvoiceService quickBooksInvoiceService,
        IInvoiceRepository invoiceRepository,
        ILogger<InvoiceQboCommandService> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _quickBooksInvoiceService = quickBooksInvoiceService ?? throw new ArgumentNullException(nameof(quickBooksInvoiceService));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<string>> CreateAsync(int userId, string realmId, CreateInvoiceRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var createResponse = await _quickBooksInvoiceService.CreateInvoiceAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(createResponse);
            if (parsed?.Invoice == null)
                throw new InvalidOperationException("Failed to create invoice in QuickBooks or response is invalid.");

            await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);
            return ApiResponse<string>.Ok(createResponse, "Invoice created successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create invoice failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to create invoice in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> UpdateAsync(int userId, string realmId, UpdateInvoiceRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var updateResponse = await _quickBooksInvoiceService.UpdateInvoiceAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(updateResponse);
            if (parsed?.Invoice == null)
                throw new InvalidOperationException("Failed to update invoice in QuickBooks or response is invalid.");

            await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);
            return ApiResponse<string>.Ok(updateResponse, "Invoice updated successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update invoice failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to update invoice in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> DeleteAsync(int userId, string realmId, DeleteInvoiceRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var deleteResponse = await _quickBooksInvoiceService.DeleteInvoiceAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(deleteResponse);
            if (parsed?.Invoice != null)
                await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);

            return ApiResponse<string>.Ok(deleteResponse, "Invoice deleted successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete invoice failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to delete invoice in QuickBooks.", new[] { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> VoidAsync(int userId, string realmId, VoidInvoiceRequest request)
    {
        try
        {
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

            var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions());

            var voidResponse = await _quickBooksInvoiceService.VoidInvoiceAsync(token.AccessToken, realmId, jsonPayload);
            var parsed = JsonSerializer.Deserialize<QuickBooksInvoiceMutationResponse>(voidResponse);
            if (parsed?.Invoice != null)
                await UpsertSingleInvoiceToDbAsync(parsed.Invoice, realmId);

            return ApiResponse<string>.Ok(voidResponse, "Invoice voided successfully in QuickBooks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Void invoice failed for user {UserId}", userId);
            return ApiResponse<string>.Fail("Failed to void invoice in QuickBooks.", new[] { ex.Message });
        }
    }

    private async Task UpsertSingleInvoiceToDbAsync(QuickBooksInvoiceDto inv, string realmId)
    {
        using var conn = _invoiceRepository.CreateOpenConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var header = QuickBooksInvoiceMapper.MapToHeader(inv, realmId);
            var lineRows = QuickBooksInvoiceMapper.MapToLineUpsertRows(inv, realmId);
            await _invoiceRepository.UpsertInvoicesAsync(new[] { header }, lineRows, conn, tx);
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
