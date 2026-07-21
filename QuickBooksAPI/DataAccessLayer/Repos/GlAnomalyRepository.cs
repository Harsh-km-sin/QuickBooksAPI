using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlAnomalyRepository : IGlAnomalyRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlAnomalyRepository(ISqlConnectionFactory db) => _db = db;

    public async Task BulkInsertAsync(IEnumerable<GlAnomaly> anomalies, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.GL_Anomalies (EntryId, RunId, UserId, AnomalyType, DetectorScore, RiskReasons, Metadata)
VALUES (@EntryId, @RunId, @UserId, @AnomalyType, @DetectorScore, @RiskReasons, @Metadata);";

        using var conn = _db.CreateConnection();

        // Batch in groups of 100 to keep statement size reasonable
        var batch = anomalies.ToList();
        for (int i = 0; i < batch.Count; i += 100)
        {
            var chunk = batch.Skip(i).Take(100);
            await conn.ExecuteAsync(_db.CreateCommand(sql, chunk, ct));
        }
    }

    public async Task<IReadOnlyList<GlAnomaly>> GetByEntryAsync(int entryId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, EntryId, RunId, UserId, AnomalyType, DetectorScore, RiskReasons, Metadata
FROM dbo.GL_Anomalies
WHERE EntryId = @EntryId
ORDER BY DetectorScore DESC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlAnomaly>(_db.CreateCommand(sql, new { EntryId = entryId }, ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<(string AnomalyType, int Count)>> GetBreakdownAsync(int runId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT AnomalyType, COUNT(*) AS [Count]
FROM dbo.GL_Anomalies
WHERE RunId = @RunId
GROUP BY AnomalyType
ORDER BY [Count] DESC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync(_db.CreateCommand(sql, new { RunId = runId }, ct));
        return rows.Select(r => ((string)r.AnomalyType, (int)r.Count)).ToList();
    }
}
