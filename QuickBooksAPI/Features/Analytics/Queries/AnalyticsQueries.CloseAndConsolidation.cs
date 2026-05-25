using Microsoft.AspNetCore.Http;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Features.Analytics.Queries;

public sealed partial class AnalyticsQueries
{
    public async Task<AnalyticsActionResult<IReadOnlyList<CloseIssueDto>>> GetCloseIssuesAsync(
        DateTime? since, string? severity, bool unresolvedOnly)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<CloseIssueDto>>.Fail(failureMessage!));
        var data = await _closeIssueService.GetIssuesAsync(userId, realmId, since, severity, unresolvedOnly, default);
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<CloseIssueDto>>.Ok(data, "Close and data-quality issues."));
    }

    public async Task<AnalyticsActionResult<object?>> ResolveCloseIssueAsync(int id)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<object?>.Fail(failureMessage!));
        await _closeIssueService.ResolveAsync(id, userId, realmId, default);
        return new(StatusCodes.Status200OK, ApiResponse<object?>.Ok(null, "Issue marked as resolved."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<EntityDto>>> GetEntitiesAsync()
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || !int.TryParse(_requestContext.UserId, out var userId))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<EntityDto>>.Fail("User context is missing."));
        var data = await _consolidationAnalyticsService.GetEntitiesForUserAsync(userId, RequestAborted);
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<EntityDto>>.Ok(data, "Entities for consolidation."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<ConsolidatedPnlRowDto>>> GetConsolidatedPnlAsync(int entityId, DateTime? from, DateTime? to)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || !int.TryParse(_requestContext.UserId, out var userId))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("User context is missing."));
        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-12);
        if (fromDate > toDate)
            return new(StatusCodes.Status400BadRequest, ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("From must be before or equal to To."));
        var query = await _consolidationAnalyticsService.GetConsolidatedPnlAsync(userId, entityId, fromDate, toDate, RequestAborted);
        if (!query.EntityBelongsToUser)
            return new(StatusCodes.Status404NotFound, ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("Entity not found."));
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Ok(query.Rows, "Consolidated P&L."));
    }
}
