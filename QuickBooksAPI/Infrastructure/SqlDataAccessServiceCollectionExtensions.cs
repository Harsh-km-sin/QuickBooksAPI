using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure;

public static class SqlDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISqlExecutor, SqlExecutor>();
        return services;
    }
}
