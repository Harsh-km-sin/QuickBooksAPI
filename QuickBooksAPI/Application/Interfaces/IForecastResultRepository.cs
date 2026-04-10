using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IForecastResultRepository
{
    Task InsertBatchAsync(IReadOnlyList<ForecastResult> results, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForecastResult>> GetByScenarioIdAsync(int scenarioId, CancellationToken cancellationToken = default);
}
