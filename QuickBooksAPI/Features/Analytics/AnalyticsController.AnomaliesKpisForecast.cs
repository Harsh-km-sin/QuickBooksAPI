using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    /// <summary>
    /// Returns anomaly events for the current company, optionally filtered by since date.
    /// </summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnomalyDto>>>> GetAnomalies(
        [FromQuery] DateTime? since = null)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<AnomalyDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<AnomalyDto>>.Fail("Invalid user id."));

        var data = await _anomalyReadService.ListAsync(userId, _requestContext.RealmId, since, HttpContext.RequestAborted);
        return Ok(ApiResponse<IReadOnlyList<AnomalyDto>>.Ok(data, "Anomaly events."));
    }

    /// <summary>
    /// Returns KPI snapshot history for the current company (e.g. for sparklines). Optional filter by names.
    /// </summary>
    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KpiSnapshotDto>>>> GetKpis(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? names = null)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("Invalid user id."));

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-6);
        if (fromDate > toDate)
            return BadRequest(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("From must be before or equal to To."));

        IReadOnlyList<string>? nameList = null;
        if (!string.IsNullOrWhiteSpace(names))
            nameList = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var data = await _kpiService.GetKpisAsync(userId, _requestContext.RealmId, fromDate, toDate, nameList);
        return Ok(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Ok(data, "KPI snapshots."));
    }

    /// <summary>
    /// Creates a forecast scenario and runs deterministic projection; returns scenario id.
    /// </summary>
    [HttpPost("forecast")]
    public async Task<ActionResult<ApiResponse<ForecastScenarioDto>>> PostForecast([FromBody] CreateForecastRequest request)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<ForecastScenarioDto>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<ForecastScenarioDto>.Fail("Invalid user id."));
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<ForecastScenarioDto>.Fail("Name is required."));

        var scenarioId = await _forecastService.CreateAndComputeAsync(
            userId, _requestContext.RealmId, request.Name,
            Math.Clamp(request.HorizonMonths, 1, 60),
            request.AssumptionsJson, null, default);

        var detail = await _forecastService.GetForecastAsync(scenarioId, userId, _requestContext.RealmId, default);
        return Ok(ApiResponse<ForecastScenarioDto>.Ok(detail!.Scenario, "Forecast scenario created and computed."));
    }

    /// <summary>
    /// Returns a forecast scenario and its results by id.
    /// </summary>
    [HttpGet("forecast/{id:int}")]
    public async Task<ActionResult<ApiResponse<ForecastDetailDto>>> GetForecast(int id)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized(ApiResponse<ForecastDetailDto>.Fail("User or realm context is missing."));
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized(ApiResponse<ForecastDetailDto>.Fail("Invalid user id."));

        var detail = await _forecastService.GetForecastAsync(id, userId, _requestContext.RealmId, default);
        if (detail == null)
            return NotFound(ApiResponse<ForecastDetailDto>.Fail("Forecast scenario not found."));
        return Ok(ApiResponse<ForecastDetailDto>.Ok(detail, "Forecast detail."));
    }
}
