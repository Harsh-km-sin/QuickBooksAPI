namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IQboSyncStateRepository
    {
        Task<DateTime?> GetLastUpdatedAfterAsync(int userId, string realmId, string entityType);
        Task UpdateLastUpdatedAfterAsync(int userId, string realmId, string entityType, DateTime lastUpdatedAfter);
        Task UpdateStatusAsync(int userId, string realmId, string entityType, string status);
    }
}
