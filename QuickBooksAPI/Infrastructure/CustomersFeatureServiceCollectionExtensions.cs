using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Customers;
using QuickBooksAPI.Features.Customers.Handlers;
using QuickBooksAPI.Services.Customers;

namespace QuickBooksAPI.Infrastructure;

public static class CustomersFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersFeature(this IServiceCollection services)
    {
        services.AddScoped<ICustomerReadService, CustomerReadService>();
        services.AddScoped<ICustomerQboSyncService, CustomerQboSyncService>();
        services.AddScoped<ICustomerQboCommandService, CustomerQboCommandService>();
        services.AddScoped<ListCustomersHandler>();
        services.AddScoped<SyncCustomersHandler>();
        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<UpdateCustomerHandler>();
        services.AddScoped<DeleteCustomerHandler>();
        services.AddScoped<ICustomerService, CustomerServiceMigrationFacade>();
        return services;
    }
}
