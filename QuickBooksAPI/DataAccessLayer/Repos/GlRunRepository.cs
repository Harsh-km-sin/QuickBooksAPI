using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlRunRepository : IGlRunRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlRunRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(GlRun run, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO dbo.GL_Runs
    (UserId, RealmId, FileName, FileType, FileSizeBytes, BlobPath, Status,
     SourceFormat, ProgressPercentage, CreatedAt)
OUTPUT INSERTED.Id
VALUES
    (@UserId, @RealmId, @FileName, @FileType, @FileSizeBytes, @BlobPath, @Status,
     @SourceFormat, 0, @CreatedAt);";

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(_db.CreateCommand(sql, new
        {
            run.UserId,
            run.RealmId,
            run.FileName,
            run.FileType,
            run.FileSizeBytes,
            run.BlobPath,
            run.Status,
            run.SourceFormat,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken));
    }

    public async Task<GlRun?> GetByIdAsync(int runId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, UserId, RealmId, FileName, FileType, FileSizeBytes, BlobPath, Status,
       ProgressPercentage, SourceFormat,
       TotalTransactions, FlaggedCount, CriticalCount, HighCount,
       MediumCount, LowCount, NormalCount,
       AvgRiskScore, MaterialExposure,
       PeriodStart, PeriodEnd,
       AiExecutiveSummary, CreatedAt, StartedAt, CompletedAt, ErrorMessage
FROM dbo.GL_Runs
WHERE Id = @RunId AND UserId = @UserId;";

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<GlRun>(
            _db.CreateCommand(sql, new { RunId = runId, UserId = userId }, cancellationToken));
    }

    public async Task<IReadOnlyList<GlRun>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, UserId, RealmId, FileName, FileType, FileSizeBytes, BlobPath, Status,
       ProgressPercentage, SourceFormat,
       TotalTransactions, FlaggedCount, CriticalCount, HighCount,
       MediumCount, LowCount, NormalCount,
       AvgRiskScore, MaterialExposure,
       PeriodStart, PeriodEnd,
       AiExecutiveSummary, CreatedAt, StartedAt, CompletedAt, ErrorMessage
FROM dbo.GL_Runs
WHERE UserId = @UserId
ORDER BY CreatedAt DESC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlRun>(
            _db.CreateCommand(sql, new { UserId = userId }, cancellationToken));
        return rows.ToList();
    }

    public async Task DeleteAsync(int runId, int userId, CancellationToken cancellationToken = default)
    {
        // GL_Transactions has ON DELETE CASCADE → GL_Anomalies, GL_Feedback, GL_ChatMessages cascade too
        const string sql = "DELETE FROM dbo.GL_Runs WHERE Id = @RunId AND UserId = @UserId;";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new { RunId = runId, UserId = userId }, cancellationToken));
    }

    public async Task RetryAsync(int runId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE dbo.GL_Runs
SET Status = 'Pending', ProgressPercentage = 0, ErrorMessage = NULL,
    StartedAt = NULL, CompletedAt = NULL
WHERE Id = @RunId AND UserId = @UserId;";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new { RunId = runId, UserId = userId }, cancellationToken));
    }
}
