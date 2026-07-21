using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlBusinessRuleRepository : IGlBusinessRuleRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlBusinessRuleRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<GlBusinessRule>> ListByUserAsync(int userId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, UserId, RuleName, Description, [Condition], Severity, IsActive, CreatedAt, UpdatedAt
FROM dbo.GL_BusinessRules
WHERE UserId = @UserId
ORDER BY CreatedAt DESC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlBusinessRule>(
            _db.CreateCommand(sql, new { UserId = userId }, ct));
        return rows.ToList();
    }

    public async Task<GlBusinessRule?> GetByIdAsync(int ruleId, int userId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, UserId, RuleName, Description, [Condition], Severity, IsActive, CreatedAt, UpdatedAt
FROM dbo.GL_BusinessRules
WHERE Id = @RuleId AND UserId = @UserId;";

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<GlBusinessRule>(
            _db.CreateCommand(sql, new { RuleId = ruleId, UserId = userId }, ct));
    }

    public async Task<int> CreateAsync(GlBusinessRule rule, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.GL_BusinessRules (UserId, RuleName, Description, [Condition], Severity, IsActive, CreatedAt, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@UserId, @RuleName, @Description, @Condition, @Severity, @IsActive, @Now, @Now);";

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(_db.CreateCommand(sql, new
        {
            rule.UserId, rule.RuleName, rule.Description,
            rule.Condition, rule.Severity, rule.IsActive,
            Now = DateTime.UtcNow
        }, ct));
    }

    public async Task UpdateAsync(GlBusinessRule rule, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE dbo.GL_BusinessRules
SET RuleName    = @RuleName,
    Description = @Description,
    [Condition] = @Condition,
    Severity    = @Severity,
    IsActive    = @IsActive,
    UpdatedAt   = @UpdatedAt
WHERE Id = @Id AND UserId = @UserId;";

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new
        {
            rule.Id, rule.UserId, rule.RuleName, rule.Description,
            rule.Condition, rule.Severity, rule.IsActive,
            UpdatedAt = DateTime.UtcNow
        }, ct));
    }

    public async Task DeleteAsync(int ruleId, int userId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.GL_BusinessRules WHERE Id = @RuleId AND UserId = @UserId;";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new { RuleId = ruleId, UserId = userId }, ct));
    }
}
