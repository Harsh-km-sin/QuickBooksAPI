using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Services;
using QuickBooksAPI.Services.Bills;
using QuickBooksAPI.Services.Customers;
using QuickBooksAPI.Services.Invoices;
using QuickBooksAPI.Services.Vendors;

namespace QuickBooksAPI.Infrastructure;

public static partial class QuickBooksApplicationServiceCollectionExtensions
{
    private static IServiceCollection AddQuickBooksEntityApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IChartOfAccountsService, ChartOfAccountsServices>();
        services.AddScoped<IProductService, ProductServices>();
        services.AddScoped<ICustomerReadService, CustomerReadService>();
        services.AddScoped<ICustomerQboSyncService, CustomerQboSyncService>();
        services.AddScoped<ICustomerQboCommandService, CustomerQboCommandService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IInvoiceReadService, InvoiceReadService>();
        services.AddScoped<IInvoiceQboSyncService, InvoiceQboSyncService>();
        services.AddScoped<IInvoiceQboCommandService, InvoiceQboCommandService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IVendorReadService, VendorReadService>();
        services.AddScoped<IVendorQboSyncService, VendorQboSyncService>();
        services.AddScoped<IVendorQboCommandService, VendorQboCommandService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IBillReadService, BillReadService>();
        services.AddScoped<IBillQboSyncService, BillQboSyncService>();
        services.AddScoped<IBillQboCommandService, BillQboCommandService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IFinancialWarehouseService, FinancialWarehouseService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        return services;
    }
}
