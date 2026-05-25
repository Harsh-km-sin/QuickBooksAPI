using Microsoft.AspNetCore.Http;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Features.Analytics.Queries;

public sealed partial class AnalyticsQueries
{
    public async Task<AnalyticsActionResult<IReadOnlyList<AnomalyDto>>> GetAnomaliesAsync(DateTime? since)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<AnomalyDto>>.Fail(failureMessage!));
        var data = await _anomalyReadService.ListAsync(userId, realmId, since, RequestAborted);
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<AnomalyDto>>.Ok(data, "Anomaly events."));
    }

    public async Task<AnalyticsActionResult<IReadOnlyList<KpiSnapshotDto>>> GetKpisAsync(DateTime? from, DateTime? to, string? names)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail(failureMessage!));
        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? toDate.AddMonths(-6);
        if (fromDate > toDate)
            return new(StatusCodes.Status400BadRequest, ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("From must be before or equal to To."));
        IReadOnlyList<string>? nameList = null;
        if (!string.IsNullOrWhiteSpace(names))
            nameList = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var data = await _kpiService.GetKpisAsync(userId, realmId, fromDate, toDate, nameList);
        return new(StatusCodes.Status200OK, ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Ok(data, "KPI snapshots."));
    }

    public async Task<AnalyticsActionResult<ForecastScenarioDto>> PostForecastAsync(CreateForecastRequest request)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<ForecastScenarioDto>.Fail(failureMessage!));
        if (string.IsNullOrWhiteSpace(request.Name))
            return new(StatusCodes.Status400BadRequest, ApiResponse<ForecastScenarioDto>.Fail("Name is required."));
        var scenarioId = await _forecastService.CreateAndComputeAsync(
            userId, realmId, request.Name,
            Math.Clamp(request.HorizonMonths, 1, 60),
            request.AssumptionsJson, null, default);
        var detail = await _forecastService.GetForecastAsync(scenarioId, userId, realmId, default);
        return new(StatusCodes.Status200OK, ApiResponse<ForecastScenarioDto>.Ok(detail!.Scenario, "Forecast scenario created and computed."));
    }

    public async Task<AnalyticsActionResult<ForecastDetailDto>> GetForecastAsync(int id)
    {
        if (!TryGetUserRealm(out var userId, out var realmId, out var failureMessage))
            return new(StatusCodes.Status401Unauthorized, ApiResponse<ForecastDetailDto>.Fail(failureMessage!));
        var detail = await _forecastService.GetForecastAsync(id, userId, realmId, default);
        if (detail == null)
            return new(StatusCodes.Status404NotFound, ApiResponse<ForecastDetailDto>.Fail("Forecast scenario not found."));
        return new(StatusCodes.Status200OK, ApiResponse<ForecastDetailDto>.Ok(detail, "Forecast detail."));
    }
}
