using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlAnomalyRepository
{
    Task BulkInsertAsync(IEnumerable<GlAnomaly> anomalies, CancellationToken ct = default);

    Task<IReadOnlyList<GlAnomaly>> GetByEntryAsync(int entryId, CancellationToken ct = default);

    /// <summary>Returns (AnomalyType, Count) pairs sorted by count descending, for the breakdown chart.</summary>
    Task<IReadOnlyList<(string AnomalyType, int Count)>> GetBreakdownAsync(int runId, CancellationToken ct = default);
}
