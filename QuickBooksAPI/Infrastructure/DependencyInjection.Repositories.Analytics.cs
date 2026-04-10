using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddAnalyticsAndWarehouseRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IFinancialWarehouseRepository>(sp =>
            new FinancialWarehouseRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IAnomalyEventRepository>(sp =>
            new AnomalyEventRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IKpiSnapshotRepository>(sp =>
            new KpiSnapshotRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IForecastScenarioRepository>(sp =>
            new ForecastScenarioRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IForecastResultRepository>(sp =>
            new ForecastResultRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<ICloseIssueRepository>(sp =>
            new CloseIssueRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IDimEntityRepository>(sp =>
            new DimEntityRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IConsolidatedPnlRepository>(sp =>
            new ConsolidatedPnlRepository(sp.GetRequiredService<ISqlConnectionFactory>()));
    }
}
