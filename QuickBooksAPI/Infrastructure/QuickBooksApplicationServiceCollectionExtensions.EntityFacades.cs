using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure;

public static partial class QuickBooksApplicationServiceCollectionExtensions
{
    private static IServiceCollection AddQuickBooksEntityApplicationServices(this IServiceCollection services)
    {
        services.AddChartOfAccountsFeature();
        services.AddProductsFeature();
        services.AddAccountingEntityFeatures();
        return services;
    }
}
