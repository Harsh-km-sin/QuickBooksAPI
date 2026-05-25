using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Services;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// Cross-entity warehouse and anomaly detection. Per-entity read/sync/command and HTTP façades are registered in <c>AddCustomersFeature</c> / <c>AddJournalEntriesFeature</c> / etc.
/// </summary>
public static class AccountingEntityFeaturesServiceCollectionExtensions
{
    public static IServiceCollection AddAccountingEntityFeatures(this IServiceCollection services)
    {
        services.AddScoped<IFinancialWarehouseService, FinancialWarehouseService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();

        services.AddCustomersFeature();
        services.AddVendorsFeature();
        services.AddBillsFeature();
        services.AddInvoicesFeature();
        services.AddJournalEntriesFeature();
        return services;
    }
}
