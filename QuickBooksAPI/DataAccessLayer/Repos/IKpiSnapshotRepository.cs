using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IKpiSnapshotRepository
    {
        Task UpsertAsync(KpiSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KpiSnapshot>> GetAsync(int userId, string realmId, DateTime from, DateTime to, IReadOnlyList<string>? kpiNames, CancellationToken cancellationToken = default);
    }
}
