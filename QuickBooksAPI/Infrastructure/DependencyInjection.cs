using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// SQL-backed repository registration (bounded-context splits: core QBO entities vs analytics/warehouse).
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "DefaultConnection connection string is missing or empty.");

        services.AddSqlDataAccess();

        services.AddCoreRepositories();
        services.AddAnalyticsAndWarehouseRepositories();

        return services;
    }
}
