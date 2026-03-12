using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IForecastResultRepository
    {
        Task InsertBatchAsync(IReadOnlyList<ForecastResult> results, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ForecastResult>> GetByScenarioIdAsync(int scenarioId, CancellationToken cancellationToken = default);
    }
}
