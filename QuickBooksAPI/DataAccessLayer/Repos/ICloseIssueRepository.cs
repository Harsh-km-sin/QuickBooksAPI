using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ICloseIssueRepository
    {
        Task InsertAsync(CloseIssue issue, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CloseIssue>> GetByUserAndRealmAsync(int userId, string realmId, DateTime? since, string? severity, bool unresolvedOnly, CancellationToken cancellationToken = default);
        Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default);
    }
}
