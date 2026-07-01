using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlFeedbackRepository : IGlFeedbackRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlFeedbackRepository(ISqlConnectionFactory db) => _db = db;

    public async Task UpsertAsync(GlFeedback f, CancellationToken ct = default)
    {
        // MERGE on EntryId: one feedback row per transaction entry.
        // Insert on first verdict; update in place on all subsequent changes.
        const string sql = @"
MERGE dbo.GL_Feedback AS target
USING (SELECT @EntryId AS EntryId) AS src ON target.EntryId = src.EntryId
WHEN MATCHED THEN
    UPDATE SET
        UserId           = @UserId,
        [Status]         = @Status,
        ResolutionStatus = @ResolutionStatus,
        AuditDecision    = @AuditDecision,
        Comments         = @Comments,
        RequiredEvidence = @RequiredEvidence,
        ResolutionNotes  = @ResolutionNotes,
        ReviewedBy       = @ReviewedBy,
        ReviewedAt       = @ReviewedAt,
        AssignedTo       = @AssignedTo
WHEN NOT MATCHED THEN
    INSERT (EntryId, RunId, UserId, [Status], ResolutionStatus,
            AuditDecision, Comments, RequiredEvidence, ResolutionNotes,
            ReviewedBy, ReviewedAt, AssignedTo)
    VALUES (@EntryId, @RunId, @UserId, @Status, @ResolutionStatus,
            @AuditDecision, @Comments, @RequiredEvidence, @ResolutionNotes,
            @ReviewedBy, @ReviewedAt, @AssignedTo);";

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new
        {
            f.EntryId, f.RunId, f.UserId,
            f.Status, f.ResolutionStatus,
            f.AuditDecision, f.Comments, f.RequiredEvidence, f.ResolutionNotes,
            f.ReviewedBy, f.ReviewedAt, f.AssignedTo
        }, ct));
    }

    public async Task<GlFeedback?> GetByEntryAsync(int entryId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, EntryId, RunId, UserId, [Status], ResolutionStatus,
       AuditDecision, Comments, RequiredEvidence, ResolutionNotes,
       ReviewedBy, ReviewedAt, AssignedTo
FROM dbo.GL_Feedback
WHERE EntryId = @EntryId;";

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<GlFeedback>(
            _db.CreateCommand(sql, new { EntryId = entryId }, ct));
    }
}
