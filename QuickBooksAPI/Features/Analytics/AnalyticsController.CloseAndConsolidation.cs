using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Features.Analytics.Queries;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    [HttpGet("close-issues")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CloseIssueDto>>>> GetCloseIssues(
        [FromQuery] DateTime? since = null,
        [FromQuery] string? severity = null,
        [FromQuery] bool unresolvedOnly = true) =>
        this.ToActionResult(await _queries.GetCloseIssuesAsync(since, severity, unresolvedOnly));

    [HttpPost("close-issues/{id:int}/resolve")]
    public async Task<ActionResult<ApiResponse<object?>>> ResolveCloseIssue(int id) =>
        this.ToActionResult(await _queries.ResolveCloseIssueAsync(id));

    [HttpGet("entities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EntityDto>>>> GetEntities() =>
        this.ToActionResult(await _queries.GetEntitiesAsync());

    [HttpGet("consolidated-pnl")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>>> GetConsolidatedPnl(
        [FromQuery] int entityId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null) =>
        this.ToActionResult(await _queries.GetConsolidatedPnlAsync(entityId, from, to));
}
