using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IForecastService
{
    Task<int> CreateAndComputeAsync(int userId, string realmId, string name, int horizonMonths, string? assumptionsJson, string? createdBy, CancellationToken cancellationToken = default);
    Task<ForecastDetailDto?> GetForecastAsync(int scenarioId, int userId, string realmId, CancellationToken cancellationToken = default);
}
