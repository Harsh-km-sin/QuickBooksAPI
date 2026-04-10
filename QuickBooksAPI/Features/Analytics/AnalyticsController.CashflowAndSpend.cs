using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    /// <summary>
    /// Returns a simple cash runway calculation for the current company.
    /// </summary>
    [HttpGet("cash-runway")]
    public async Task<ActionResult<ApiResponse<CashRunwayResult>>> GetCashRunway()
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
        {
            return Unauthorized(ApiResponse<CashRunwayResult>.Fail("User or realm context is missing."));
        }

        if (!int.TryParse(_requestContext.UserId, out var userId))
        {
            return Unauthorized(ApiResponse<CashRunwayResult>.Fail("Invalid user id."));
        }

        var result = await _cashRunwayService.GetRunwayAsync(userId, _requestContext.RealmId);
        return Ok(ApiResponse<CashRunwayResult>.Ok(result, "Cash runway calculated."));
    }

    /// <summary>
    /// Returns top vendors by spend for the current company (e.g. period=30 or 90 days).
    /// </summary>
    [HttpGet("vendor-spend/top")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VendorSpendDto>>>> GetVendorSpendTop(
        [FromQuery] int period = 30,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<VendorSpendDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<VendorSpendDto>>.Fail("Invalid user id."));

        var data = await _vendorAnalyticsService.GetTopVendorsAsync(userId, _requestContext.RealmId, Math.Max(1, period), Math.Clamp(limit, 1, 100));
        return Ok(ApiResponse<IReadOnlyList<VendorSpendDto>>.Ok(data, "Top vendors by spend."));
    }

    /// <summary>
    /// Returns vendor spend summary for the current company over a date range.
    /// </summary>
    [HttpGet("vendor-spend/summary")]
    public async Task<ActionResult<ApiResponse<VendorSpendSummaryDto>>> GetVendorSpendSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<VendorSpendSummaryDto>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<VendorSpendSummaryDto>.Fail("Invalid user id."));

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-1);
        if (fromDate > toDate)
            return BadRequest(ApiResponse<VendorSpendSummaryDto>.Fail("From must be before or equal to To."));

        var data = await _vendorAnalyticsService.GetSummaryAsync(userId, _requestContext.RealmId, fromDate, toDate);
        return Ok(ApiResponse<VendorSpendSummaryDto>.Ok(data, "Vendor spend summary."));
    }

    /// <summary>
    /// Returns customer profitability for the current company over a date range.
    /// </summary>
    [HttpGet("customer-profitability")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>>> GetCustomerProfitability(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int top = 50)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("Invalid user id."));

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-1);
        if (fromDate > toDate)
            return BadRequest(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("From must be before or equal to To."));

        var data = await _customerProfitabilityService.GetCustomerProfitabilityAsync(userId, _requestContext.RealmId, fromDate, toDate, Math.Clamp(top, 1, 200));
        return Ok(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Ok(data, "Customer profitability."));
    }

    /// <summary>
    /// Returns monthly revenue and expenses for the current company (e.g. last 12 months for charts).
    /// </summary>
    [HttpGet("revenue-expenses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>>> GetRevenueExpenses(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("Invalid user id."));

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-12);
        if (fromDate > toDate)
            return BadRequest(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("From must be before or equal to To."));

        var data = await _revenueExpensesService.GetMonthlyAsync(userId, _requestContext.RealmId, fromDate, toDate);
        return Ok(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Ok(data, "Revenue vs expenses by month."));
    }
}
