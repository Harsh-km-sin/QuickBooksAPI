using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Invoices;
using QuickBooksAPI.Features.Invoices.Handlers;
using QuickBooksAPI.Services.Invoices;

namespace QuickBooksAPI.Infrastructure;

public static class InvoicesFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddInvoicesFeature(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceReadService, InvoiceReadService>();
        services.AddScoped<IInvoiceQboSyncService, InvoiceQboSyncService>();
        services.AddScoped<IInvoiceQboCommandService, InvoiceQboCommandService>();
        services.AddScoped<ListInvoicesHandler>();
        services.AddScoped<SyncInvoicesHandler>();
        services.AddScoped<CreateInvoiceHandler>();
        services.AddScoped<UpdateInvoiceHandler>();
        services.AddScoped<DeleteInvoiceHandler>();
        services.AddScoped<VoidInvoiceHandler>();
        services.AddScoped<IInvoiceService, InvoiceServiceMigrationFacade>();
        services.AddScoped<ITermService, QuickBooksAPI.Services.Terms.TermService>();
        return services;
    }
}
