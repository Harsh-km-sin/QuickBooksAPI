using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class CloseIssueRepository : ICloseIssueRepository
    {
        private readonly string _connectionString;

        public CloseIssueRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InsertAsync(CloseIssue issue, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.close_issues (UserId, RealmId, IssueType, Severity, Details, DetectedAt)
VALUES (@UserId, @RealmId, @IssueType, @Severity, @Details, @DetectedAt);";
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    issue.UserId,
                    issue.RealmId,
                    issue.IssueType,
                    issue.Severity,
                    issue.Details,
                    issue.DetectedAt
                }, cancellationToken: cancellationToken));
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

            using var connection = new SqlConnection(_connectionString);
            var parameters = new { UserId = userId, RealmId = realmId, Since = since, Severity = severity };
            var rows = await connection.QueryAsync<CloseIssue>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
            return rows?.ToList() ?? new List<CloseIssue>();
        }

        public async Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE dbo.close_issues SET ResolvedAt = SYSUTCDATETIME() WHERE Id = @Id AND UserId = @UserId AND RealmId = @RealmId;";
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id, UserId = userId, RealmId = realmId }, cancellationToken: cancellationToken));
        }
    }
}
