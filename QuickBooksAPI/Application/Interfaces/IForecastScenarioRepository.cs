using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IForecastScenarioRepository
{
    Task<int> InsertAsync(ForecastScenario scenario, CancellationToken cancellationToken = default);
    Task<ForecastScenario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ForecastScenario?> GetByIdAndUserRealmAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int scenarioId, string status, CancellationToken cancellationToken = default);
}
