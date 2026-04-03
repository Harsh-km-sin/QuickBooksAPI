using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces.Analytics;

public sealed class ConsolidatedPnlQueryResult
{
    public bool EntityBelongsToUser { get; init; }

    public IReadOnlyList<ConsolidatedPnlRowDto> Rows { get; init; } = Array.Empty<ConsolidatedPnlRowDto>();
}

public interface IConsolidationAnalyticsService
{
    Task<IReadOnlyList<EntityDto>> GetEntitiesForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<ConsolidatedPnlQueryResult> GetConsolidatedPnlAsync(
        int userId,
        int entityId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
