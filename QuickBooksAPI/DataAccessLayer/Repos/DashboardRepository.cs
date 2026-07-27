using Dapper;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DashboardRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<DashboardStatsDto?> GetStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(
            "dbo.GetDashboardStats",
            new { UserId = userId, RealmId = realmId },
            commandType: CommandType.StoredProcedure,
            commandTimeout: _connectionFactory.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<DashboardStatsDto>(cmd);
    }
}
