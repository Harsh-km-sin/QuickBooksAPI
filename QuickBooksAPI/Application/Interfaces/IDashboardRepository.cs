using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatsDto?> GetStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default);
}
