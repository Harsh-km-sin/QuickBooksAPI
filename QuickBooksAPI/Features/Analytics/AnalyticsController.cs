using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Interfaces.Analytics;

namespace QuickBooksAPI.Features.Analytics;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class AnalyticsController : ControllerBase
{
    private readonly ICashRunwayService _cashRunwayService;
    private readonly IVendorAnalyticsService _vendorAnalyticsService;
    private readonly ICustomerProfitabilityService _customerProfitabilityService;
    private readonly IRevenueExpensesService _revenueExpensesService;
    private readonly IAnomalyReadService _anomalyReadService;
    private readonly IKpiService _kpiService;
    private readonly IForecastService _forecastService;
    private readonly ICloseIssueService _closeIssueService;
    private readonly IConsolidationAnalyticsService _consolidationAnalyticsService;
    private readonly IRequestContext _requestContext;

    public AnalyticsController(
        ICashRunwayService cashRunwayService,
        IVendorAnalyticsService vendorAnalyticsService,
        ICustomerProfitabilityService customerProfitabilityService,
        IRevenueExpensesService revenueExpensesService,
        IAnomalyReadService anomalyReadService,
        IKpiService kpiService,
        IForecastService forecastService,
        ICloseIssueService closeIssueService,
        IConsolidationAnalyticsService consolidationAnalyticsService,
        IRequestContext requestContext)
    {
        _cashRunwayService = cashRunwayService;
        _vendorAnalyticsService = vendorAnalyticsService;
        _customerProfitabilityService = customerProfitabilityService;
        _revenueExpensesService = revenueExpensesService;
        _anomalyReadService = anomalyReadService;
        _kpiService = kpiService;
        _forecastService = forecastService;
        _closeIssueService = closeIssueService;
        _consolidationAnalyticsService = consolidationAnalyticsService;
        _requestContext = requestContext;
    }
}
