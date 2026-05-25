using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Features.Analytics.Queries;

public readonly record struct AnalyticsActionResult<T>(int StatusCode, ApiResponse<T> Response);

public static class AnalyticsActionResultMapper
{
    public static ActionResult<ApiResponse<T>> ToActionResult<T>(this ControllerBase c, AnalyticsActionResult<T> r) =>
        r.StatusCode switch
        {
            StatusCodes.Status200OK => c.Ok(r.Response),
            StatusCodes.Status400BadRequest => c.BadRequest(r.Response),
            StatusCodes.Status401Unauthorized => c.Unauthorized(r.Response),
            StatusCodes.Status404NotFound => c.NotFound(r.Response),
            _ => c.StatusCode(r.StatusCode, r.Response)
        };
}
