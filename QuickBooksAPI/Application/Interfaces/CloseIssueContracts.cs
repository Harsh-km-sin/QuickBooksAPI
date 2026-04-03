using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICloseIssueService
{
    Task<IReadOnlyList<CloseIssueDto>> GetIssuesAsync(int userId, string realmId, DateTime? since, string? severity, bool unresolvedOnly, CancellationToken cancellationToken = default);
    Task ResolveAsync(int id, int userId, string realmId, CancellationToken cancellationToken = default);
}
