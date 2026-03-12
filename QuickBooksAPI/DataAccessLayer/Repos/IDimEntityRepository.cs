using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IDimEntityRepository
    {
        Task<IReadOnlyList<DimEntity>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DimEntity>> GetChildrenAsync(int parentEntityId, CancellationToken cancellationToken = default);
        Task<DimEntity?> GetByUserAndRealmAsync(int userId, string realmId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DimEntity>> GetParentEntitiesAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> UpsertAsync(DimEntity entity, CancellationToken cancellationToken = default);
    }
}
