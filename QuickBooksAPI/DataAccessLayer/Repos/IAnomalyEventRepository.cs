using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IAnomalyEventRepository
    {
        Task InsertAsync(AnomalyEvent anomaly, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AnomalyEvent>> GetByUserAndRealmAsync(int userId, string realmId, DateTime? since, CancellationToken cancellationToken = default);
    }
}
