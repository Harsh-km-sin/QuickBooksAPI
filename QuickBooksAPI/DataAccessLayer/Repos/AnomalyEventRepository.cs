using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class AnomalyEventRepository : IAnomalyEventRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public AnomalyEventRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task InsertAsync(AnomalyEvent anomaly, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.anomaly_events (UserId, RealmId, [Type], Severity, Details, DetectedAt)
VALUES (@UserId, @RealmId, @Type, @Severity, @Details, @DetectedAt);";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                _connectionFactory.CreateCommand(sql, new
                {
                    anomaly.UserId,
                    anomaly.RealmId,
                    anomaly.Type,
                    anomaly.Severity,
                    anomaly.Details,
                    anomaly.DetectedAt
                }, cancellationToken));
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

            using var connection = _connectionFactory.CreateConnection();
            var parameters = new { UserId = userId, RealmId = realmId, Since = since };
            var rows = await connection.QueryAsync<AnomalyEvent>(
                _connectionFactory.CreateCommand(sql, parameters, cancellationToken));
            return rows?.ToList() ?? new List<AnomalyEvent>();
        }
    }
}
