using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlRunRepository
{
    Task<int> CreateAsync(GlRun run, CancellationToken cancellationToken = default);
    Task<GlRun?> GetByIdAsync(int runId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlRun>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int runId, int userId, CancellationToken cancellationToken = default);
    Task RetryAsync(int runId, int userId, CancellationToken cancellationToken = default);
}
