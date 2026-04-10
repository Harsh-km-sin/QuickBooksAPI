using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IConsolidatedPnlRepository
{
    Task UpsertAsync(FactConsolidatedPnl row, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FactConsolidatedPnl>> GetByEntityAndRangeAsync(int entityId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
