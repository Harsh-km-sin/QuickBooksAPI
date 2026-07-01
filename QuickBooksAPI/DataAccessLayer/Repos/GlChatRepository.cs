using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlChatRepository : IGlChatRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlChatRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<GlChatMessage>> GetHistoryAsync(int runId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, RunId, UserId, [Role], Content, Metadata, CreatedAt
FROM dbo.GL_ChatMessages
WHERE RunId = @RunId
ORDER BY CreatedAt ASC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlChatMessage>(
            _db.CreateCommand(sql, new { RunId = runId }, ct));
        return rows.ToList();
    }

    public async Task AddMessageAsync(GlChatMessage msg, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.GL_ChatMessages (RunId, UserId, [Role], Content, Metadata, CreatedAt)
VALUES (@RunId, @UserId, @Role, @Content, @Metadata, @CreatedAt);";

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new
        {
            msg.RunId, msg.UserId, msg.Role, msg.Content, msg.Metadata,
            CreatedAt = DateTime.UtcNow
        }, ct));
    }

    public async Task ClearHistoryAsync(int runId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.GL_ChatMessages WHERE RunId = @RunId;";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new { RunId = runId }, ct));
    }
}
