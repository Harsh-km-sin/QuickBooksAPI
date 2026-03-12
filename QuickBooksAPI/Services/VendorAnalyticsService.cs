using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface IVendorAnalyticsService
    {
        Task<IReadOnlyList<VendorSpendDto>> GetTopVendorsAsync(int userId, string realmId, int periodDays, int limit, CancellationToken cancellationToken = default);
        Task<VendorSpendSummaryDto> GetSummaryAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Exposes vendor spend intelligence from the financial warehouse for CFO analytics.
    /// </summary>
    public class VendorAnalyticsService : IVendorAnalyticsService
    {
        private readonly IFinancialWarehouseRepository _warehouse;

        public VendorAnalyticsService(IFinancialWarehouseRepository warehouse)
        {
            _warehouse = warehouse;
        }

        public async Task<IReadOnlyList<VendorSpendDto>> GetTopVendorsAsync(int userId, string realmId, int periodDays, int limit, CancellationToken cancellationToken = default)
        {
            var periodStart = DateTime.UtcNow.Date.AddDays(-Math.Max(1, periodDays));
            var rows = await _warehouse.GetVendorSpendTopAsync(userId, realmId, periodDays, limit, cancellationToken);
            return rows.Select(r => new VendorSpendDto
            {
                VendorName = r.VendorName,
                TotalSpend = r.TotalSpend,
                BillCount = r.BillCount,
                LastBillDate = r.LastBillDate,
                PeriodStart = periodStart
            }).ToList();
        }

        public async Task<VendorSpendSummaryDto> GetSummaryAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var row = await _warehouse.GetVendorSpendSummaryAsync(userId, realmId, from, to, cancellationToken);
            return new VendorSpendSummaryDto
            {
                TotalSpend = row.TotalSpend,
                VendorCount = row.VendorCount,
                BillCount = row.BillCount,
                From = from.Date,
                To = to.Date
            };
        }
    }
}
