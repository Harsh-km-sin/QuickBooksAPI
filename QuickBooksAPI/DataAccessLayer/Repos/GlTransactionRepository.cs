using System.Text;
using Dapper;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlTransactionRepository : IGlTransactionRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlTransactionRepository(ISqlConnectionFactory db) => _db = db;

    private static readonly string SelectColumns = @"
Id, RunId, TransactionDate, AccountId, AccountName, AccountType, PostingType, Amount,
EntityName, Description, SourceType, CreatedBy, CreatedDate, JournalEntryId,
CompositeRiskScore, RiskTier, AnomalyFlags, ZScore, IsReviewed, ReviewedAt, ReviewNote";

    public async Task<PagedResult<GlTransaction>> ListPagedAsync(int runId, GlTransactionFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("WHERE RunId = @RunId");
        var p = new DynamicParameters();
        p.Add("RunId", runId);
        p.Add("Skip", (filter.Page - 1) * filter.PageSize);
        p.Add("PageSize", filter.PageSize);

        if (!string.IsNullOrWhiteSpace(filter.RiskTier)) { where.Append(" AND RiskTier = @RiskTier"); p.Add("RiskTier", filter.RiskTier); }
        if (!string.IsNullOrWhiteSpace(filter.AccountName)) { where.Append(" AND AccountName LIKE @AccountName"); p.Add("AccountName", $"%{filter.AccountName}%"); }
        if (!string.IsNullOrWhiteSpace(filter.EntityName)) { where.Append(" AND EntityName LIKE @EntityName"); p.Add("EntityName", $"%{filter.EntityName}%"); }
        if (filter.DateFrom.HasValue) { where.Append(" AND TransactionDate >= @DateFrom"); p.Add("DateFrom", filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue)); }
        if (filter.DateTo.HasValue) { where.Append(" AND TransactionDate <= @DateTo"); p.Add("DateTo", filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue)); }
        if (filter.AmountMin.HasValue) { where.Append(" AND Amount >= @AmountMin"); p.Add("AmountMin", filter.AmountMin.Value); }
        if (filter.AmountMax.HasValue) { where.Append(" AND Amount <= @AmountMax"); p.Add("AmountMax", filter.AmountMax.Value); }
        if (filter.UnreviewedOnly) { where.Append(" AND IsReviewed = 0"); }

        var countSql = $"SELECT COUNT(*) FROM dbo.GL_Transactions {where};";
        var itemsSql = $@"
SELECT {SelectColumns}
FROM dbo.GL_Transactions {where}
ORDER BY CompositeRiskScore DESC, TransactionDate DESC
OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = _db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(_db.CreateCommand(countSql, p, cancellationToken));
        var items = await conn.QueryAsync<GlTransaction>(_db.CreateCommand(itemsSql, p, cancellationToken));

        return new PagedResult<GlTransaction>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<GlTransaction?> GetByIdAsync(int id, int runId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.GL_Transactions WHERE Id = @Id AND RunId = @RunId;";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<GlTransaction>(_db.CreateCommand(sql, new { Id = id, RunId = runId }, cancellationToken));
    }

    public async Task MarkReviewedAsync(int id, int runId, string? note, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE dbo.GL_Transactions
SET IsReviewed = 1, ReviewedAt = @ReviewedAt, ReviewNote = @ReviewNote
WHERE Id = @Id AND RunId = @RunId;";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new { Id = id, RunId = runId, ReviewedAt = DateTime.UtcNow, ReviewNote = note }, cancellationToken));
    }

    public async Task<IReadOnlyList<GlAccountStats>> GetAccountStatsAsync(int runId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, RunId, AccountName, TransactionCount, TotalAmount, AvgAmount, StdDev, MinAmount, MaxAmount, OutlierCount
FROM dbo.GL_AccountStats
WHERE RunId = @RunId
ORDER BY OutlierCount DESC, TotalAmount DESC;";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlAccountStats>(_db.CreateCommand(sql, new { RunId = runId }, cancellationToken));
        return rows.ToList();
    }

    public async Task<Dictionary<string, int>> GetRiskDistributionAsync(int runId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT RiskTier, COUNT(*) AS Cnt
FROM dbo.GL_Transactions
WHERE RunId = @RunId AND RiskTier IS NOT NULL
GROUP BY RiskTier;";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync(_db.CreateCommand(sql, new { RunId = runId }, cancellationToken));
        return rows.ToDictionary(r => (string)r.RiskTier, r => (int)r.Cnt);
    }

    public async Task<IReadOnlyList<GlTransaction>> GetTopFlaggedAsync(int runId, int count, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT TOP (@Count) {SelectColumns}
FROM dbo.GL_Transactions
WHERE RunId = @RunId AND CompositeRiskScore IS NOT NULL AND IsReviewed = 0
ORDER BY CompositeRiskScore DESC;";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlTransaction>(_db.CreateCommand(sql, new { RunId = runId, Count = count }, cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<GlTransaction>> GetAllForExportAsync(int runId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {SelectColumns}
FROM dbo.GL_Transactions
WHERE RunId = @RunId
ORDER BY CompositeRiskScore DESC, TransactionDate DESC;";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlTransaction>(_db.CreateCommand(sql, new { RunId = runId }, cancellationToken));
        return rows.ToList();
    }
}
