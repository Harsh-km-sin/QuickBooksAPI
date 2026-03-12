using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IConsolidatedPnlRepository
    {
        Task UpsertAsync(FactConsolidatedPnl row, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FactConsolidatedPnl>> GetByEntityAndRangeAsync(int entityId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}
