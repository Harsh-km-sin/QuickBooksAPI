using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Analytics.Queries;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    [HttpGet("cash-runway")]
    public async Task<ActionResult<ApiResponse<CashRunwayResult>>> GetCashRunway() =>
        this.ToActionResult(await _queries.GetCashRunwayAsync());

    [HttpGet("vendor-spend/top")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VendorSpendDto>>>> GetVendorSpendTop(
        [FromQuery] int period = 30,
        [FromQuery] int limit = 10) =>
        this.ToActionResult(await _queries.GetVendorSpendTopAsync(period, limit));

    [HttpGet("vendor-spend/summary")]
    public async Task<ActionResult<ApiResponse<VendorSpendSummaryDto>>> GetVendorSpendSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null) =>
        this.ToActionResult(await _queries.GetVendorSpendSummaryAsync(from, to));

    [HttpGet("customer-profitability")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>>> GetCustomerProfitability(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int top = 50) =>
        this.ToActionResult(await _queries.GetCustomerProfitabilityAsync(from, to, top));

    [HttpGet("revenue-expenses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>>> GetRevenueExpenses(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null) =>
        this.ToActionResult(await _queries.GetRevenueExpensesAsync(from, to));
}
