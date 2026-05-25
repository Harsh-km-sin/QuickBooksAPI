using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products;
using QuickBooksAPI.Features.Products.Handlers;

namespace QuickBooksAPI.Infrastructure;

public static class ProductsFeatureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Products feature handlers and the migration façade for <see cref="IProductService"/>.
    /// </summary>
    public static IServiceCollection AddProductsFeature(this IServiceCollection services)
    {
        services.AddScoped<SyncProductsHandler>();
        services.AddScoped<ListProductsHandler>();
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeleteProductHandler>();
        services.AddScoped<IProductService, ProductServiceMigrationFacade>();
        return services;
    }
}
