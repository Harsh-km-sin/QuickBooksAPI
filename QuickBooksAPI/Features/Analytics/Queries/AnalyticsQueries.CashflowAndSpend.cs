using Microsoft.AspNetCore.Http;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.Analytics.Queries;

public sealed partial class AnalyticsQueries
{
    public async Task<AnalyticsActionResult<CashRunwayResult>> GetCashRunwayAsync()
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<CashRunwayResult>.Fail(failureMessage!));
        var result = await _cashRunwayService.GetRunwayAsync(userId, realmId);
        return new(StatusCodes.Status200OK, ApiResponse<CashRunwayResult>.Ok(result, "Cash runway calculated."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<VendorSpendDto>>> GetVendorSpendTopAsync(int period, int limit)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<VendorSpendDto>>.Fail(failureMessage!));
        var data = await _vendorAnalyticsService.GetTopVendorsAsync(userId, realmId, Math.Max(1, period), Math.Clamp(limit, 1, 100));
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<VendorSpendDto>>.Ok(data, "Top vendors by spend."));
    }

    public async Task<AnalyticsActionResult<VendorSpendSummaryDto>> GetVendorSpendSummaryAsync(DateTime? from, DateTime? to)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<VendorSpendSummaryDto>.Fail(failureMessage!));
        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-1);
        if (fromDate > toDate)
            return new(StatusCodes.Status400BadRequest, ApiResponse<VendorSpendSummaryDto>.Fail("From must be before or equal to To."));
        var data = await _vendorAnalyticsService.GetSummaryAsync(userId, realmId, fromDate, toDate);
        return new(StatusCodes.Status200OK, ApiResponse<VendorSpendSummaryDto>.Ok(data, "Vendor spend summary."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<CustomerProfitabilityDto>>> GetCustomerProfitabilityAsync(
        DateTime? from, DateTime? to, int top)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail(failureMessage!));
        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-1);
        if (fromDate > toDate)
            return new(StatusCodes.Status400BadRequest, ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("From must be before or equal to To."));
        var data = await _customerProfitabilityService.GetCustomerProfitabilityAsync(userId, realmId, fromDate, toDate, Math.Clamp(top, 1, 200));
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Ok(data, "Customer profitability."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<RevenueExpensesMonthlyDto>>> GetRevenueExpensesAsync(DateTime? from, DateTime? to)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail(failureMessage!));
        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-12);
        if (fromDate > toDate)
            return new(StatusCodes.Status400BadRequest, ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("From must be before or equal to To."));
        var data = await _revenueExpensesService.GetMonthlyAsync(userId, realmId, fromDate, toDate);
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Ok(data, "Revenue vs expenses by month."));
    }
}
