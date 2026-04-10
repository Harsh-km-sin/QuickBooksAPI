using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces.Analytics;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.Analytics;

public sealed class ConsolidationAnalyticsService : IConsolidationAnalyticsService
{
    private readonly IDimEntityRepository _dimEntityRepository;
    private readonly IConsolidatedPnlRepository _consolidatedPnlRepository;

    public ConsolidationAnalyticsService(
        IDimEntityRepository dimEntityRepository,
        IConsolidatedPnlRepository consolidatedPnlRepository)
    {
        _dimEntityRepository = dimEntityRepository ?? throw new ArgumentNullException(nameof(dimEntityRepository));
        _consolidatedPnlRepository = consolidatedPnlRepository ?? throw new ArgumentNullException(nameof(consolidatedPnlRepository));
    }

    public async Task<IReadOnlyList<EntityDto>> GetEntitiesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var entities = await _dimEntityRepository.GetByUserIdAsync(userId, cancellationToken);
        return entities.Select(e => new EntityDto
        {
            Id = e.Id,
            Name = e.Name,
            RealmId = e.RealmId,
            IsConsolidatedNode = e.IsConsolidatedNode
        }).ToList();
    }

    public async Task<ConsolidatedPnlQueryResult> GetConsolidatedPnlAsync(
        int userId,
        int entityId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dimEntityRepository.GetByUserIdAsync(userId, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.Id == entityId);
        if (entity == null)
        {
            return new ConsolidatedPnlQueryResult { EntityBelongsToUser = false, Rows = Array.Empty<ConsolidatedPnlRowDto>() };
        }

        var rows = await _consolidatedPnlRepository.GetByEntityAndRangeAsync(entityId, from, to, cancellationToken);
        var dtos = rows.Select(r => new ConsolidatedPnlRowDto
        {
            PeriodStart = r.PeriodStart,
            PeriodEnd = r.PeriodEnd,
            Revenue = r.Revenue,
            Expenses = r.Expenses,
            NetIncome = r.NetIncome
        }).ToList();

        return new ConsolidatedPnlQueryResult { EntityBelongsToUser = true, Rows = dtos };
    }
}
