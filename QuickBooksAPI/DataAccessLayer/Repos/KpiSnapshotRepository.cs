using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class KpiSnapshotRepository : IKpiSnapshotRepository
    {
        private readonly string _connectionString;

        public KpiSnapshotRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task UpsertAsync(KpiSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            const string sql = @"
MERGE dbo.kpi_snapshot AS target
USING (SELECT @UserId AS UserId, @RealmId AS RealmId, @SnapshotDate AS SnapshotDate, @KpiName AS KpiName, @Period AS Period) AS source
ON target.UserId = source.UserId AND target.RealmId = source.RealmId AND target.SnapshotDate = source.SnapshotDate AND target.KpiName = source.KpiName AND target.Period = source.Period
WHEN MATCHED THEN
    UPDATE SET KpiValue = @KpiValue, MetadataJson = @MetadataJson
WHEN NOT MATCHED THEN
    INSERT (UserId, RealmId, SnapshotDate, KpiName, KpiValue, Period, MetadataJson)
    VALUES (@UserId, @RealmId, @SnapshotDate, @KpiName, @KpiValue, @Period, @MetadataJson);";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    snapshot.UserId,
                    snapshot.RealmId,
                    SnapshotDate = snapshot.SnapshotDate.Date,
                    snapshot.KpiName,
                    snapshot.KpiValue,
                    snapshot.Period,
                    snapshot.MetadataJson
                }, cancellationToken: cancellationToken));
        }

        public async Task<IReadOnlyList<KpiSnapshot>> GetAsync(int userId, string realmId, DateTime from, DateTime to, IReadOnlyList<string>? kpiNames, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT Id, UserId, RealmId, SnapshotDate, KpiName, KpiValue, Period, MetadataJson
FROM dbo.kpi_snapshot
WHERE UserId = @UserId AND RealmId = @RealmId
  AND SnapshotDate >= @From AND SnapshotDate <= @To";
            if (kpiNames != null && kpiNames.Count > 0)
            {
                // Build IN clause; use a table-valued parameter or dynamic. Simple: pass as comma-separated and split, or use multiple params.
                sql += " AND KpiName IN @KpiNames";
            }
            sql += " ORDER BY SnapshotDate, KpiName;";

            using var connection = new SqlConnection(_connectionString);
            var parameters = new { UserId = userId, RealmId = realmId, From = from.Date, To = to.Date, KpiNames = kpiNames };
            var rows = await connection.QueryAsync<KpiSnapshot>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
            return rows?.ToList() ?? new List<KpiSnapshot>();
        }
    }
}
