using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickBooksAPI.Infrastructure.HealthChecks;

namespace QuickBooksAPI.Infrastructure;

public static class QuickBooksApiHealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Registers liveness/self checks. Optional database check is added when <c>HealthChecks:IncludeDatabase</c> is true
    /// (requires <see cref="DependencyInjection.AddInfrastructure"/> so <see cref="QuickBooksAPI.DataAccessLayer.Sql.ISqlConnectionFactory"/> is available).
    /// </summary>
    public static IServiceCollection AddQuickBooksApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: new[] { "live", "ready" });

        var includeDb = configuration.GetValue("HealthChecks:IncludeDatabase", false);
        if (includeDb)
            healthChecks.AddCheck<SqlConnectionHealthCheck>(
                "database",
                tags: new[] { "ready" });

        return services;
    }
}
