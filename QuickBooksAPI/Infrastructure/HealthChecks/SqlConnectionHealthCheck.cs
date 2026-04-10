using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure.HealthChecks;

/// <summary>
/// Lightweight SQL connectivity probe (SELECT 1). Registered only when <c>HealthChecks:IncludeDatabase</c> is true.
/// </summary>
public sealed class SqlConnectionHealthCheck : IHealthCheck
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlConnectionHealthCheck(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.CommandTimeout = 5;
            await cmd.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
