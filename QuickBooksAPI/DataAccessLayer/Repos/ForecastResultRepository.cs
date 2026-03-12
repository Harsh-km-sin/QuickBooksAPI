using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ForecastResultRepository : IForecastResultRepository
    {
        private readonly string _connectionString;

        public ForecastResultRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InsertBatchAsync(IReadOnlyList<ForecastResult> results, CancellationToken cancellationToken = default)
        {
            if (results == null || results.Count == 0) return;

            const string sql = @"
INSERT INTO dbo.forecast_results (ScenarioId, PeriodStart, Revenue, Expenses, NetIncome, CashBalance, RunwayMonths, MetadataJson)
VALUES (@ScenarioId, @PeriodStart, @Revenue, @Expenses, @NetIncome, @CashBalance, @RunwayMonths, @MetadataJson);";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            foreach (var r in results)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(sql, new
                    {
                        r.ScenarioId,
                        PeriodStart = r.PeriodStart.Date,
                        r.Revenue,
                        r.Expenses,
                        r.NetIncome,
                        r.CashBalance,
                        r.RunwayMonths,
                        r.MetadataJson
                    }, cancellationToken: cancellationToken));
            }
        }

        public async Task<IReadOnlyList<ForecastResult>> GetByScenarioIdAsync(int scenarioId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, ScenarioId, PeriodStart, Revenue, Expenses, NetIncome, CashBalance, RunwayMonths, MetadataJson
FROM dbo.forecast_results WHERE ScenarioId = @ScenarioId ORDER BY PeriodStart;";
            using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<ForecastResult>(
                new CommandDefinition(sql, new { ScenarioId = scenarioId }, cancellationToken: cancellationToken));
            return rows?.ToList() ?? new List<ForecastResult>();
        }
    }
}
