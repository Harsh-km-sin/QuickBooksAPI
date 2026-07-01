using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Features.GlReview;

[ApiController]
[Route("api/gl-review/runs/{runId:int}/analytics")]
[Authorize]
public class GlAnalyticsController : ControllerBase
{
    private readonly IGlRunRepository _runs;
    private readonly IGlAnomalyRepository _anomalies;
    private readonly ISqlConnectionFactory _db;
    private readonly IRequestContext _ctx;

    public GlAnalyticsController(
        IGlRunRepository runs,
        IGlAnomalyRepository anomalies,
        ISqlConnectionFactory db,
        IRequestContext ctx)
    {
        _runs = runs;
        _anomalies = anomalies;
        _db = db;
        _ctx = ctx;
    }

    // GET /api/gl-review/runs/{runId}/analytics/period-metrics
    [HttpGet("period-metrics")]
    public async Task<IActionResult> PeriodMetrics(int runId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, ct);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        const string sql = @"
SELECT
    FORMAT(TransactionDate, 'yyyy-MM')       AS Month,
    SUM(Amount)                               AS TotalAmount,
    SUM(CASE WHEN RiskTier NOT IN ('Normal', 'Low') THEN Amount ELSE 0 END) AS FlaggedAmount,
    SUM(CASE WHEN RiskTier NOT IN ('Normal', 'Low') THEN 1 ELSE 0 END)      AS FlaggedCount,
    ISNULL(AVG(CAST(RiskScore AS FLOAT)), 0)  AS AvgRiskScore
FROM dbo.GL_Transactions
WHERE RunId = @RunId
GROUP BY FORMAT(TransactionDate, 'yyyy-MM')
ORDER BY Month;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlPeriodMetricDto>(_db.CreateCommand(sql, new { RunId = runId }, ct));
        return Ok(ApiResponse<IEnumerable<GlPeriodMetricDto>>.Ok(rows));
    }

    // GET /api/gl-review/runs/{runId}/analytics/entity-risk
    [HttpGet("entity-risk")]
    public async Task<IActionResult> EntityRisk(int runId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, ct);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        const string sql = @"
SELECT
    EntityName                          AS Party,
    MAX(ISNULL(RiskScore, 0))           AS MaxRiskScore,
    SUM(CASE WHEN RiskTier NOT IN ('Normal', 'Low') THEN 1 ELSE 0 END) AS FlaggedCount,
    SUM(Amount)                         AS TotalAmount
FROM dbo.GL_Transactions
WHERE RunId = @RunId
GROUP BY EntityName
HAVING MAX(ISNULL(RiskScore, 0)) > 0
ORDER BY MaxRiskScore DESC;";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<GlEntityRiskDto>(_db.CreateCommand(sql, new { RunId = runId }, ct));
        return Ok(ApiResponse<IEnumerable<GlEntityRiskDto>>.Ok(rows));
    }

    // GET /api/gl-review/runs/{runId}/analytics/anomaly-breakdown
    [HttpGet("anomaly-breakdown")]
    public async Task<IActionResult> AnomalyBreakdown(int runId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, ct);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var breakdown = await _anomalies.GetBreakdownAsync(runId, ct);
        var total = breakdown.Sum(b => b.Count);

        var result = breakdown.Select(b => new GlAnomalyBreakdownDto
        {
            AnomalyType = b.AnomalyType,
            Count = b.Count,
            Percentage = total > 0 ? Math.Round((double)b.Count / total * 100, 1) : 0
        }).ToList();

        return Ok(ApiResponse<IReadOnlyList<GlAnomalyBreakdownDto>>.Ok(result));
    }

    private bool TryGetUserId(out int userId, out string? error)
    {
        userId = 0;
        error = null;
        if (string.IsNullOrEmpty(_ctx.UserId) || !int.TryParse(_ctx.UserId, out userId))
        {
            error = "User context is missing. Please sign in.";
            return false;
        }
        return true;
    }
}
