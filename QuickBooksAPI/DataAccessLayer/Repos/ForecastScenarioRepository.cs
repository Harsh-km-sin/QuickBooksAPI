using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ForecastScenarioRepository : IForecastScenarioRepository
    {
        private readonly string _connectionString;

        public ForecastScenarioRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<int> InsertAsync(ForecastScenario scenario, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.forecast_scenarios (UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status)
VALUES (@UserId, @RealmId, @Name, @CreatedAtUtc, @CreatedBy, @HorizonMonths, @AssumptionsJson, @Status);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = new SqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new
                {
                    scenario.UserId,
                    scenario.RealmId,
                    scenario.Name,
                    scenario.CreatedAtUtc,
                    scenario.CreatedBy,
                    scenario.HorizonMonths,
                    scenario.AssumptionsJson,
                    scenario.Status
                }, cancellationToken: cancellationToken));
            return id;
        }

        public async Task<ForecastScenario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status
FROM dbo.forecast_scenarios WHERE Id = @Id;";
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ForecastScenario>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        }

        public async Task<ForecastScenario?> GetByIdAndUserRealmAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, Name, CreatedAtUtc, CreatedBy, HorizonMonths, AssumptionsJson, Status
FROM dbo.forecast_scenarios WHERE Id = @Id AND UserId = @UserId AND RealmId = @RealmId;";
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ForecastScenario>(
                new CommandDefinition(sql, new { Id = id, UserId = userId, RealmId = realmId }, cancellationToken: cancellationToken));
        }

        public async Task UpdateStatusAsync(int scenarioId, string status, CancellationToken cancellationToken = default)
        {
            const string sql = "UPDATE dbo.forecast_scenarios SET Status = @Status WHERE Id = @ScenarioId;";
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { ScenarioId = scenarioId, Status = status }, cancellationToken: cancellationToken));
        }
    }
}
