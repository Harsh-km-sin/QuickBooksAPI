using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;
using QuickBooksService.Services.AuthTransport;

namespace QuickBooksAPI.Integrations.QuickBooks;

/// <summary>
/// Registers QuickBooks Online HTTP/OAuth adapters from <c>QuickBooksService</c>. Application façades in <c>Services/</c> depend on these types.
/// </summary>
public static class QuickBooksIntegrationServiceCollectionExtensions
{
    /// <summary>Registers QuickBooks Online provider adapters (alias for composition roots).</summary>
    public static IServiceCollection AddAccountingProviders(this IServiceCollection services) =>
        AddQuickBooksOnlineIntegration(services);

    public static IServiceCollection AddQuickBooksOnlineIntegration(this IServiceCollection services)
    {
        services.AddScoped<IVendorAccountingSyncGateway, QuickBooksVendorAccountingSyncGateway>();
        services.AddScoped<IProductAccountingSyncGateway, QuickBooksProductAccountingSyncGateway>();
        services.AddScoped<IProductAccountingCommandGateway, QuickBooksProductAccountingCommandGateway>();
        services.AddScoped<IChartOfAccountsAccountingGateway, QuickBooksChartOfAccountsAccountingGateway>();
        services.AddScoped<IJournalEntryAccountingGateway, QuickBooksJournalEntryAccountingGateway>();

        services.AddScoped<QboTokenExchangeClient>();
        services.AddScoped<QboTokenRefreshClient>();
        services.AddScoped<QboTokenRevokeClient>();
        services.AddScoped<QboCompanyInfoClient>();
        services.AddScoped<IQuickBooksAuthService, QuickBooksAuthService>();
        services.AddScoped<IQuickBooksChartOfAccountsService, QuickBooksChartOfAccountsService>();
        services.AddScoped<IQuickBooksProductService, QuickBooksProductService>();
        services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
        services.AddScoped<IQuickBooksJournalEntryService, QuickBooksJournalEntryService>();
        services.AddScoped<IQuickBooksInvoiceService, QuickBooksInvoiceService>();
        services.AddScoped<IQuickBooksVendorService, QuickBooksVendorService>();
        services.AddScoped<IQuickBooksBillService, QuickBooksBillService>();
        return services;
    }
}
