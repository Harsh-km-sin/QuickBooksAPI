using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    /// <summary>
    /// Returns close and data-quality issues for the current company.
    /// </summary>
    [HttpGet("close-issues")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CloseIssueDto>>>> GetCloseIssues(
        [FromQuery] DateTime? since = null,
        [FromQuery] string? severity = null,
        [FromQuery] bool unresolvedOnly = true)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<CloseIssueDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<CloseIssueDto>>.Fail("Invalid user id."));

        var data = await _closeIssueService.GetIssuesAsync(userId, _requestContext.RealmId, since, severity, unresolvedOnly, default);
        return Ok(ApiResponse<IReadOnlyList<CloseIssueDto>>.Ok(data, "Close and data-quality issues."));
    }

    /// <summary>
    /// Marks a close issue as resolved.
    /// </summary>
    [HttpPost("close-issues/{id:int}/resolve")]
    public async Task<ActionResult<ApiResponse<object?>>> ResolveCloseIssue(int id)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<object?>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<object?>.Fail("Invalid user id."));

        await _closeIssueService.ResolveAsync(id, userId, _requestContext.RealmId, default);
        return Ok(ApiResponse<object?>.Ok(null, "Issue marked as resolved."));
    }

    /// <summary>
    /// Returns entities (companies/subsidiaries) for the current user for consolidation.
    /// </summary>
    [HttpGet("entities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EntityDto>>>> GetEntities()
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || !int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<EntityDto>>.Fail("User context is missing."));

        var data = await _consolidationAnalyticsService.GetEntitiesForUserAsync(userId, HttpContext.RequestAborted);
        return Ok(ApiResponse<IReadOnlyList<EntityDto>>.Ok(data, "Entities for consolidation."));
    }

    /// <summary>
    /// Returns consolidated P&amp;L for a parent entity (must belong to current user).
    /// </summary>
    [HttpGet("consolidated-pnl")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>>> GetConsolidatedPnl(
        [FromQuery] int entityId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || !int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("User context is missing."));

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-12);
        if (fromDate > toDate)
            return BadRequest(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("From must be before or equal to To."));

        var query = await _consolidationAnalyticsService.GetConsolidatedPnlAsync(
            userId, entityId, fromDate, toDate, HttpContext.RequestAborted);
        if (!query.EntityBelongsToUser)
            return NotFound(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("Entity not found."));

        return Ok(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Ok(query.Rows, "Consolidated P&L."));
    }
}
