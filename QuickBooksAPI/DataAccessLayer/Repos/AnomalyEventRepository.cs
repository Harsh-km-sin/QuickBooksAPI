using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class AnomalyEventRepository : IAnomalyEventRepository
    {
        private readonly string _connectionString;

        public AnomalyEventRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InsertAsync(AnomalyEvent anomaly, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.anomaly_events (UserId, RealmId, [Type], Severity, Details, DetectedAt)
VALUES (@UserId, @RealmId, @Type, @Severity, @Details, @DetectedAt);";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    anomaly.UserId,
                    anomaly.RealmId,
                    anomaly.Type,
                    anomaly.Severity,
                    anomaly.Details,
                    anomaly.DetectedAt
                }, cancellationToken: cancellationToken));
        }

        public async Task<IReadOnlyList<AnomalyEvent>> GetByUserAndRealmAsync(int userId, string realmId, DateTime? since, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT Id, UserId, RealmId, [Type], Severity, Details, DetectedAt
FROM dbo.anomaly_events
WHERE UserId = @UserId AND RealmId = @RealmId";
            if (since.HasValue)
                sql += " AND DetectedAt >= @Since";
            sql += " ORDER BY DetectedAt DESC;";

            using var connection = new SqlConnection(_connectionString);
            var parameters = new { UserId = userId, RealmId = realmId, Since = since };
            var rows = await connection.QueryAsync<AnomalyEvent>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
            return rows?.ToList() ?? new List<AnomalyEvent>();
        }
    }
}
