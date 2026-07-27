using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;
using QuickBooksService.Services.AuthTransport;
using QuickBooksService.Services.Resilience;

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
        services.AddScoped<QboPreferencesClient>();
        services.AddScoped<IQuickBooksAuthService, QuickBooksAuthService>();
        services.AddScoped<IQuickBooksChartOfAccountsService, QuickBooksChartOfAccountsService>();
        services.AddScoped<IQuickBooksProductService, QuickBooksProductService>();
        services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
        services.AddScoped<IQuickBooksJournalEntryService, QuickBooksJournalEntryService>();
        services.AddScoped<IQuickBooksInvoiceService, QuickBooksInvoiceService>();
        services.AddScoped<IQuickBooksTermService, QuickBooksTermService>();
        services.AddScoped<IQuickBooksVendorService, QuickBooksVendorService>();
        services.AddScoped<IQuickBooksBillService, QuickBooksBillService>();

        // Reports client only: QBO's Reports endpoint has a tighter rate limit (200 req/min) than the
        // rest of the API, so this is the first (and only, for now) QBO client with retry/backoff on 429.
        // QboRetryHandler itself is generic — attach it to other named clients later if we backport this.
        services.AddTransient<QboRetryHandler>();
        services.AddHttpClient(QuickBooksReportsService.HttpClientName)
            .AddHttpMessageHandler<QboRetryHandler>();
        services.AddScoped<IQuickBooksReportsService, QuickBooksReportsService>();

        return services;
    }
}
