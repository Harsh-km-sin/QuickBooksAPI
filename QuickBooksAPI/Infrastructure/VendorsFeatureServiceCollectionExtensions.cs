using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Vendors;
using QuickBooksAPI.Features.Vendors.Handlers;
using QuickBooksAPI.Services.Vendors;

namespace QuickBooksAPI.Infrastructure;

public static class VendorsFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddVendorsFeature(this IServiceCollection services)
    {
        services.AddScoped<IVendorReadService, VendorReadService>();
        services.AddScoped<IVendorQboSyncService, VendorQboSyncService>();
        services.AddScoped<IVendorQboCommandService, VendorQboCommandService>();
        services.AddScoped<ListVendorsHandler>();
        services.AddScoped<SyncVendorsHandler>();
        services.AddScoped<CreateVendorHandler>();
        services.AddScoped<UpdateVendorHandler>();
        services.AddScoped<SoftDeleteVendorHandler>();
        services.AddScoped<IVendorService, VendorServiceMigrationFacade>();
        return services;
    }
}
