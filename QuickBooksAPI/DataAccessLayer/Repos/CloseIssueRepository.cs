using Dapper;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class CloseIssueRepository : ICloseIssueRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public CloseIssueRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task InsertAsync(CloseIssue issue, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.close_issues (UserId, RealmId, IssueType, Severity, Details, DetectedAt)
VALUES (@UserId, @RealmId, @IssueType, @Severity, @Details, @DetectedAt);";
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                _connectionFactory.CreateCommand(sql, new
                {
                    issue.UserId,
                    issue.RealmId,
                    issue.IssueType,
                    issue.Severity,
                    issue.Details,
                    issue.DetectedAt
                }, cancellationToken));
        }

        public async Task<IReadOnlyList<CloseIssue>> GetByUserAndRealmAsync(int userId, string realmId, DateTime? since, string? severity, bool unresolvedOnly, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT Id, UserId, RealmId, IssueType, Severity, Details, DetectedAt, ResolvedAt
FROM dbo.close_issues
WHERE UserId = @UserId AND RealmId = @RealmId";
            if (since.HasValue)
                sql += " AND DetectedAt >= @Since";
            if (!string.IsNullOrWhiteSpace(severity))
                sql += " AND Severity = @Severity";
            if (unresolvedOnly)
                sql += " AND ResolvedAt IS NULL";
            sql += " ORDER BY DetectedAt DESC;";

            using var connection = _connectionFactory.CreateConnection();
            var parameters = new { UserId = userId, RealmId = realmId, Since = since, Severity = severity };
            var rows = await connection.QueryAsync<CloseIssue>(
                _connectionFactory.CreateCommand(sql, parameters, cancellationToken));
            return rows?.ToList() ?? new List<CloseIssue>();
        }

        public async Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE dbo.close_issues SET ResolvedAt = SYSUTCDATETIME() WHERE Id = @Id AND UserId = @UserId AND RealmId = @RealmId;";
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                _connectionFactory.CreateCommand(sql, new { Id = id, UserId = userId, RealmId = realmId }, cancellationToken));
        }
    }
}
