using Microsoft.AspNetCore.Http;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Interfaces.Analytics;

namespace QuickBooksAPI.Features.Analytics.Queries;

/// <summary>
/// Analytics query handlers (HTTP-agnostic); <see cref="AnalyticsController"/> maps results to MVC.
/// </summary>
public sealed partial class AnalyticsQueries
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyticsQueries(
        ICashRunwayService cashRunwayService,
        IVendorAnalyticsService vendorAnalyticsService,
        ICustomerProfitabilityService customerProfitabilityService,
        IRevenueExpensesService revenueExpensesService,
        IAnomalyReadService anomalyReadService,
        IKpiService kpiService,
        IForecastService forecastService,
        ICloseIssueService closeIssueService,
        IConsolidationAnalyticsService consolidationAnalyticsService,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor)
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
        _httpContextAccessor = httpContextAccessor;
    }

    private CancellationToken RequestAborted =>
        _httpContextAccessor.HttpContext?.RequestAborted ?? default;

    private bool TryGetUserRealm(out int userId, out string realmId, out string? failureMessage)
    {
        userId = 0;
        realmId = "";
        failureMessage = null;
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
        {
            failureMessage = "User or realm context is missing.";
            return false;
        }

        if (!int.TryParse(_requestContext.UserId, out userId))
        {
            failureMessage = "Invalid user id.";
            return false;
        }

        realmId = _requestContext.RealmId!;
        return true;
    }
}
