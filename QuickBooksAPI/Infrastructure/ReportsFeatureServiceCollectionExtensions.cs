using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Reports;
using QuickBooksAPI.Features.Reports.Handlers;
using QuickBooksAPI.Services.Auth;
using QuickBooksAPI.Services.Reports;

namespace QuickBooksAPI.Infrastructure;

public static class ReportsFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddReportsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQboCompanyMetadataService, QboCompanyMetadataService>();
        services.AddScoped<IReportReadService, ReportReadService>();
        services.AddScoped<IReportQboSyncService, ReportQboSyncService>();
        services.AddScoped<GetReportHandler>();
        services.AddScoped<ListReportPeriodsHandler>();
        services.AddScoped<SyncReportsHandler>();
        services.AddScoped<IReportService, ReportServiceMigrationFacade>();
        return services;
    }
}
