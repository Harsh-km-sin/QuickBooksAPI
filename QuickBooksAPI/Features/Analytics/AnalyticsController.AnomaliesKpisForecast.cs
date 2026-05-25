using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Features.Analytics.Queries;

namespace QuickBooksAPI.Features.Analytics;

public partial class AnalyticsController
{
    [HttpGet("anomalies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AnomalyDto>>>> GetAnomalies([FromQuery] DateTime? since = null) =>
        this.ToActionResult(await _queries.GetAnomaliesAsync(since));

    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KpiSnapshotDto>>>> GetKpis(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? names = null) =>
        this.ToActionResult(await _queries.GetKpisAsync(from, to, names));

    [HttpPost("forecast")]
    public async Task<ActionResult<ApiResponse<ForecastScenarioDto>>> PostForecast([FromBody] CreateForecastRequest request) =>
        this.ToActionResult(await _queries.PostForecastAsync(request));

    [HttpGet("forecast/{id:int}")]
    public async Task<ActionResult<ApiResponse<ForecastDetailDto>>> GetForecast(int id) =>
        this.ToActionResult(await _queries.GetForecastAsync(id));
}
