using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Bills;
using QuickBooksAPI.Features.Bills.Handlers;
using QuickBooksAPI.Services.Bills;

namespace QuickBooksAPI.Infrastructure;

public static class BillsFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddBillsFeature(this IServiceCollection services)
    {
        services.AddScoped<IBillReadService, BillReadService>();
        services.AddScoped<IBillQboSyncService, BillQboSyncService>();
        services.AddScoped<IBillQboCommandService, BillQboCommandService>();
        services.AddScoped<ListBillsHandler>();
        services.AddScoped<SyncBillsHandler>();
        services.AddScoped<CreateBillHandler>();
        services.AddScoped<UpdateBillHandler>();
        services.AddScoped<DeleteBillHandler>();
        services.AddScoped<IBillService, BillServiceMigrationFacade>();
        return services;
    }
}
