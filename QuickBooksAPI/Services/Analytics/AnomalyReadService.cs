using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces.Analytics;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services.Analytics;

public sealed class AnomalyReadService : IAnomalyReadService
{
    private readonly IAnomalyEventRepository _repository;

    public AnomalyReadService(IAnomalyEventRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<AnomalyDto>> ListAsync(int userId, string realmId, DateTime? since, CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetByUserAndRealmAsync(userId, realmId, since, cancellationToken);
        return events.Select(e => new AnomalyDto
        {
            Id = e.Id,
            Type = e.Type,
            Severity = e.Severity,
            Details = e.Details,
            DetectedAt = e.DetectedAt
        }).ToList();
    }
}
