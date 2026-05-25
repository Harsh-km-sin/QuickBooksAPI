using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.ChartOfAccounts;
using QuickBooksAPI.Features.ChartOfAccounts.Handlers;

namespace QuickBooksAPI.Infrastructure;

public static class ChartOfAccountsFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddChartOfAccountsFeature(this IServiceCollection services)
    {
        services.AddScoped<ListChartOfAccountsHandler>();
        services.AddScoped<SyncChartOfAccountsHandler>();
        services.AddScoped<IChartOfAccountsService, ChartOfAccountsServiceMigrationFacade>();
        return services;
    }
}
