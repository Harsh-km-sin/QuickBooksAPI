using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Interfaces.Analytics;
using QuickBooksAPI.Features.Analytics.Queries;
using QuickBooksAPI.Features.CloseIssues;
using QuickBooksAPI.Features.Forecast;
using QuickBooksAPI.Services;
using QuickBooksAPI.Services.Analytics;
using QuickBooksAPI.Services.CfoAssistant;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// Warehouse analytics, forecasting, close issues, CFO assistant (intent handlers), and consolidation reads — API host (and optional worker analytics).
/// </summary>
public static class QuickBooksAnalyticsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddQuickBooksAnalyticsApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AnalyticsQueries>();
        services.AddScoped<ICashRunwayService, CashRunwayService>();
        services.AddScoped<IVendorAnalyticsService, VendorAnalyticsService>();
        services.AddScoped<ICustomerProfitabilityService, CustomerProfitabilityService>();
        services.AddScoped<IRevenueExpensesService, RevenueExpensesService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<ICloseIssueService, CloseIssueService>();
        services.AddScoped<IAnomalyReadService, AnomalyReadService>();
        services.AddScoped<IConsolidationAnalyticsService, ConsolidationAnalyticsService>();

        services.AddScoped<ICfoAssistantIntentHandler, CfoAssistantRunwayIntentHandler>();
        services.AddScoped<ICfoAssistantIntentHandler, CfoAssistantRevenueExpensesIntentHandler>();
        services.AddScoped<ICfoAssistantIntentHandler, CfoAssistantCustomerProfitIntentHandler>();
        services.AddScoped<ICfoAssistantIntentHandler, CfoAssistantVendorSpendIntentHandler>();
        services.AddHttpClient(CfoAssistantService.AzureOpenAiHttpClientName);
        services.AddScoped<ICfoAssistantService, CfoAssistantService>();
        return services;
    }
}
