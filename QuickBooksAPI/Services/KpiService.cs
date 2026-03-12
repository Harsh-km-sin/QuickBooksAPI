using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface IKpiService
    {
        Task<IReadOnlyList<KpiSnapshotDto>> GetKpisAsync(int userId, string realmId, DateTime from, DateTime to, IReadOnlyList<string>? names, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Exposes KPI snapshot history for CFO dashboard sparklines.
    /// </summary>
    public class KpiService : IKpiService
    {
        private readonly IKpiSnapshotRepository _repo;

        public KpiService(IKpiSnapshotRepository repo)
        {
            _repo = repo;
        }

        public async Task<IReadOnlyList<KpiSnapshotDto>> GetKpisAsync(int userId, string realmId, DateTime from, DateTime to, IReadOnlyList<string>? names, CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAsync(userId, realmId, from, to, names, cancellationToken);
            return rows.Select(r => new KpiSnapshotDto
            {
                SnapshotDate = r.SnapshotDate,
                KpiName = r.KpiName,
                KpiValue = r.KpiValue,
                Period = r.Period
            }).ToList();
        }
    }
}
