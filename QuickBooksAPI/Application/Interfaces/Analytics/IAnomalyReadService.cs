using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces.Analytics;

public interface IAnomalyReadService
{
    Task<IReadOnlyList<AnomalyDto>> ListAsync(int userId, string realmId, DateTime? since, CancellationToken cancellationToken = default);
}
