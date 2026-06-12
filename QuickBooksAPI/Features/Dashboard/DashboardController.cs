using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Analytics.Queries;

namespace QuickBooksAPI.Features.Dashboard;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IRequestContext _requestContext;
    private readonly ICustomerReadService _customerReadService;
    private readonly IVendorReadService _vendorReadService;
    private readonly IProductService _productService;
    private readonly IInvoiceReadService _invoiceReadService;
    private readonly IBillReadService _billReadService;
    private readonly AnalyticsQueries _queries;
    private readonly IConnectedCompanyQueryService _connectedCompanyQueryService;

    public DashboardController(
        IRequestContext requestContext,
        ICustomerReadService customerReadService,
        IVendorReadService vendorReadService,
        IProductService productService,
        IInvoiceReadService invoiceReadService,
        IBillReadService billReadService,
        AnalyticsQueries queries,
        IConnectedCompanyQueryService connectedCompanyQueryService)
    {
        _requestContext = requestContext;
        _customerReadService = customerReadService;
        _vendorReadService = vendorReadService;
        _productService = productService;
        _invoiceReadService = invoiceReadService;
        _billReadService = billReadService;
        _queries = queries;
        _connectedCompanyQueryService = connectedCompanyQueryService;
    }

    /// <summary>
    /// Returns all dashboard data in a single request. All queries run in parallel.
    /// Replaces the 13+ individual calls the frontend previously made on page load.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int vendorPeriod = 30,
        [FromQuery] int vendorLimit = 10,
        [FromQuery] int customerTop = 50)
    {
        if (!int.TryParse(_requestContext.UserId, out var userId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<DashboardSummaryDto>.Fail("User context missing."));

        var realmId = _requestContext.RealmId!;
        var defaultQuery = new ListQueryParams();
        var now = DateTime.UtcNow.Date;
        var from1Month = now.AddMonths(-1);
        var from6Months = now.AddMonths(-6);
        var from12Months = now.AddMonths(-12);

        var customersTask   = _customerReadService.ListPagedAsync(userId, realmId, defaultQuery);
        var vendorsTask     = _vendorReadService.ListPagedAsync(userId, realmId, defaultQuery);
        var productsTask    = _productService.ListProductsAsync(defaultQuery);
        var invoicesTask    = _invoiceReadService.ListPagedAsync(realmId, defaultQuery);
        var billsTask       = _billReadService.ListPagedAsync(realmId, defaultQuery);
        var cashRunwayTask  = _queries.GetCashRunwayAsync();
        var topVendorsTask  = _queries.GetVendorSpendTopAsync(vendorPeriod, vendorLimit);
        var profitTask      = _queries.GetCustomerProfitabilityAsync(from1Month, now, customerTop);
        var revenueTask     = _queries.GetRevenueExpensesAsync(from12Months, now);
        var anomaliesTask   = _queries.GetAnomaliesAsync(null);
        var closeTask       = _queries.GetCloseIssuesAsync(null, null, true);
        var kpisTask        = _queries.GetKpisAsync(from6Months, now, null);
        var companiesTask   = _connectedCompanyQueryService.GetConnectedCompaniesAsync(userId);

        await Task.WhenAll(
            customersTask, vendorsTask, productsTask, invoicesTask, billsTask,
            cashRunwayTask, topVendorsTask, profitTask, revenueTask,
            anomaliesTask, closeTask, kpisTask, companiesTask);

        var summary = new DashboardSummaryDto
        {
            Customers             = customersTask.Result.Data,
            Vendors               = vendorsTask.Result.Data,
            Products              = productsTask.Result.Data,
            Invoices              = invoicesTask.Result.Data,
            Bills                 = billsTask.Result.Data,
            CashRunway            = cashRunwayTask.Result.Response.Data,
            TopVendors            = topVendorsTask.Result.Response.Data ?? Array.Empty<VendorSpendDto>(),
            CustomerProfitability = profitTask.Result.Response.Data ?? Array.Empty<CustomerProfitabilityDto>(),
            RevenueExpenses       = revenueTask.Result.Response.Data ?? Array.Empty<RevenueExpensesMonthlyDto>(),
            Anomalies             = anomaliesTask.Result.Response.Data ?? Array.Empty<AnomalyDto>(),
            CloseIssues           = closeTask.Result.Response.Data ?? Array.Empty<CloseIssueDto>(),
            Kpis                  = kpisTask.Result.Response.Data ?? Array.Empty<KpiSnapshotDto>(),
            ConnectedCompanies    = companiesTask.Result.Data ?? Array.Empty<ConnectedCompanyDto>()
        };

        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary, "Dashboard summary."));
    }
}
