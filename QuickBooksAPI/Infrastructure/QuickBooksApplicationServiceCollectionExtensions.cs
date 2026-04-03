using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Services;
using QuickBooksAPI.Services.Auth;
using QuickBooksAPI.Services.Bills;
using QuickBooksAPI.Services.Customers;
using QuickBooksAPI.Services.Invoices;
using QuickBooksAPI.Services.Vendors;
using QuickBooksService.Services;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// Application-layer registrations shared by the API host and SyncWorker (auth, QBO HTTP adapters, entity façades).
/// </summary>
public static class QuickBooksApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddQuickBooksAuthAndEntityApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IUserSignUpValidator, UserSignUpRequestValidator>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserLoginService, UserLoginService>();
        services.AddScoped<IQboConnectionService, QboConnectionService>();
        services.AddScoped<IQboTokenLifecycleService, QboTokenLifecycleService>();
        services.AddScoped<IConnectedCompanyQueryService, ConnectedCompanyQueryService>();
        services.AddScoped<IAuthService, AuthServices>();
        services.AddScoped<IQuickBooksAuthService, QuickBooksAuthService>();
        services.AddScoped<IChartOfAccountsService, ChartOfAccountsServices>();
        services.AddScoped<IQuickBooksChartOfAccountsService, QuickBooksChartOfAccountsService>();
        services.AddScoped<IProductService, ProductServices>();
        services.AddScoped<IQuickBooksProductService, QuickBooksProductService>();
        services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
        services.AddScoped<ICustomerReadService, CustomerReadService>();
        services.AddScoped<ICustomerQboSyncService, CustomerQboSyncService>();
        services.AddScoped<ICustomerQboCommandService, CustomerQboCommandService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IQuickBooksJournalEntryService, QuickBooksJournalEntryService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IQuickBooksInvoiceService, QuickBooksInvoiceService>();
        services.AddScoped<IInvoiceReadService, InvoiceReadService>();
        services.AddScoped<IInvoiceQboSyncService, InvoiceQboSyncService>();
        services.AddScoped<IInvoiceQboCommandService, InvoiceQboCommandService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IQuickBooksVendorService, QuickBooksVendorService>();
        services.AddScoped<IVendorReadService, VendorReadService>();
        services.AddScoped<IVendorQboSyncService, VendorQboSyncService>();
        services.AddScoped<IVendorQboCommandService, VendorQboCommandService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IQuickBooksBillService, QuickBooksBillService>();
        services.AddScoped<IBillReadService, BillReadService>();
        services.AddScoped<IBillQboSyncService, BillQboSyncService>();
        services.AddScoped<IBillQboCommandService, BillQboCommandService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IFinancialWarehouseService, FinancialWarehouseService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        return services;
    }
}
