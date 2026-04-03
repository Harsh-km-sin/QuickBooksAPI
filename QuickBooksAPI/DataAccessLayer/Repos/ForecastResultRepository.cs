using Dapper;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ForecastResultRepository : IForecastResultRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ForecastResultRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task InsertBatchAsync(IReadOnlyList<ForecastResult> results, CancellationToken cancellationToken = default)
        {
            if (results == null || results.Count == 0) return;

            const string sql = @"
INSERT INTO dbo.forecast_results (ScenarioId, PeriodStart, Revenue, Expenses, NetIncome, CashBalance, RunwayMonths, MetadataJson)
VALUES (@ScenarioId, @PeriodStart, @Revenue, @Expenses, @NetIncome, @CashBalance, @RunwayMonths, @MetadataJson);";

            using var connection = _connectionFactory.CreateConnection();
            foreach (var r in results)
            {
                await connection.ExecuteAsync(
                    _connectionFactory.CreateCommand(sql, new
                    {
                        r.ScenarioId,
                        PeriodStart = r.PeriodStart.Date,
                        r.Revenue,
                        r.Expenses,
                        r.NetIncome,
                        r.CashBalance,
                        r.RunwayMonths,
                        r.MetadataJson
                    }, cancellationToken));
            }
        }

        public async Task<IReadOnlyList<ForecastResult>> GetByScenarioIdAsync(int scenarioId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, ScenarioId, PeriodStart, Revenue, Expenses, NetIncome, CashBalance, RunwayMonths, MetadataJson
FROM dbo.forecast_results WHERE ScenarioId = @ScenarioId ORDER BY PeriodStart;";
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.QueryAsync<ForecastResult>(
                _connectionFactory.CreateCommand(sql, new { ScenarioId = scenarioId }, cancellationToken));
            return rows?.ToList() ?? new List<ForecastResult>();
        }
    }
}
