using Dapper;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ConsolidatedPnlRepository : IConsolidatedPnlRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ConsolidatedPnlRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task UpsertAsync(FactConsolidatedPnl row, CancellationToken cancellationToken = default)
        {
            const string sql = @"
MERGE dbo.fact_consolidated_pnl AS t
USING (SELECT @EntityId AS EntityId, @PeriodStart AS PeriodStart) AS s ON t.EntityId = s.EntityId AND t.PeriodStart = s.PeriodStart
WHEN MATCHED THEN
  UPDATE SET PeriodEnd = @PeriodEnd, Revenue = @Revenue, Expenses = @Expenses, NetIncome = @NetIncome, FxRateApplied = @FxRateApplied, MetadataJson = @MetadataJson
WHEN NOT MATCHED THEN
  INSERT (EntityId, PeriodStart, PeriodEnd, Revenue, Expenses, NetIncome, FxRateApplied, MetadataJson)
  VALUES (@EntityId, @PeriodStart, @PeriodEnd, @Revenue, @Expenses, @NetIncome, @FxRateApplied, @MetadataJson);";
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(_connectionFactory.CreateCommand(sql, new
            {
                row.EntityId,
                row.PeriodStart,
                row.PeriodEnd,
                row.Revenue,
                row.Expenses,
                row.NetIncome,
                row.FxRateApplied,
                row.MetadataJson
            }, cancellationToken));
        }

        public async Task<IReadOnlyList<FactConsolidatedPnl>> GetByEntityAndRangeAsync(int entityId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, EntityId, PeriodStart, PeriodEnd, Revenue, Expenses, NetIncome, FxRateApplied, MetadataJson
FROM dbo.fact_consolidated_pnl
WHERE EntityId = @EntityId AND PeriodStart >= @From AND PeriodStart <= @To
ORDER BY PeriodStart;";
            using var connection = _connectionFactory.CreateConnection();
            var list = await connection.QueryAsync<FactConsolidatedPnl>(_connectionFactory.CreateCommand(sql, new { EntityId = entityId, From = from, To = to }, cancellationToken));
            return list?.ToList() ?? new List<FactConsolidatedPnl>();
        }
    }
}
