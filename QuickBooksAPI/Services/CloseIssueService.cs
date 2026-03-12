using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface ICloseIssueService
    {
        Task<IReadOnlyList<CloseIssueDto>> GetIssuesAsync(int userId, string realmId, DateTime? since, string? severity, bool unresolvedOnly, CancellationToken cancellationToken = default);
        Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default);
    }

    public class CloseIssueService : ICloseIssueService
    {
        private readonly ICloseIssueRepository _repo;

        public CloseIssueService(ICloseIssueRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<IReadOnlyList<CloseIssueDto>> GetIssuesAsync(int userId, string realmId, DateTime? since, string? severity, bool unresolvedOnly, CancellationToken cancellationToken = default)
        {
            var list = await _repo.GetByUserAndRealmAsync(userId, realmId, since, severity, unresolvedOnly, cancellationToken);
            return list.Select(e => new CloseIssueDto
            {
                Id = e.Id,
                IssueType = e.IssueType,
                Severity = e.Severity,
                Details = e.Details,
                DetectedAt = e.DetectedAt,
                ResolvedAt = e.ResolvedAt
            }).ToList();
        }

        public async Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            await _repo.ResolveAsync(id, userId, realmId, cancellationToken);
        }
    }
}
