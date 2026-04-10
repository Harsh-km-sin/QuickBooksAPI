using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ForecastScenarioRepository : IForecastScenarioRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ForecastScenarioRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<int> InsertAsync(ForecastScenario scenario, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.forecast_scenarios (UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status)
VALUES (@UserId, @RealmId, @Name, @CreatedAtUtc, @CreatedBy, @HorizonMonths, @AssumptionsJson, @Status);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(
                _connectionFactory.CreateCommand(sql, new
                {
                    scenario.UserId,
                    scenario.RealmId,
                    scenario.Name,
                    scenario.CreatedAtUtc,
                    scenario.CreatedBy,
                    scenario.HorizonMonths,
                    scenario.AssumptionsJson,
                    scenario.Status
                }, cancellationToken));
            return id;
        }

        public async Task<ForecastScenario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status
FROM dbo.forecast_scenarios WHERE Id = @Id;";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ForecastScenario>(
                _connectionFactory.CreateCommand(sql, new { Id = id }, cancellationToken));
        }

        public async Task<ForecastScenario?> GetByIdAndUserRealmAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status
FROM dbo.forecast_scenarios WHERE Id = @Id AND UserId = @UserId AND RealmId = @RealmId;";
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ForecastScenario>(
                _connectionFactory.CreateCommand(sql, new { Id = id, UserId = userId, RealmId = realmId }, cancellationToken));
        }

        public async Task UpdateStatusAsync(int scenarioId, string status, CancellationToken cancellationToken = default)
        {
            const string sql = "UPDATE dbo.forecast_scenarios SET Status = @Status WHERE Id = @ScenarioId;";
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                _connectionFactory.CreateCommand(sql, new { ScenarioId = scenarioId, Status = status }, cancellationToken));
        }
    }
}
