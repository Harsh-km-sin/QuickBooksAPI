using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface IAnomalyDetectionService
    {
        Task DetectAsync(int userId, string realmId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Runs rules-based anomaly detection after warehouse rebuild and persists results to anomaly_events.
    /// </summary>
    public class AnomalyDetectionService : IAnomalyDetectionService
    {
        private const decimal VendorSpikeFactor = 1.5m;
        private const decimal LargeTransactionFactor = 2.0m;
        private const int OverdueReceivablesThreshold = 5;
        private const int OverdueDays = 90;

        private readonly IFinancialWarehouseRepository _warehouse;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IAnomalyEventRepository _anomalyRepo;

        public AnomalyDetectionService(
            IFinancialWarehouseRepository warehouse,
            IInvoiceRepository invoiceRepo,
            IAnomalyEventRepository anomalyRepo)
        {
            _warehouse = warehouse;
            _invoiceRepo = invoiceRepo;
            _anomalyRepo = anomalyRepo;
        }

        public async Task DetectAsync(int userId, string realmId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow.Date;
            var periodStart = new DateTime(now.Year, now.Month, 1);
            var twoMonthsAgo = periodStart.AddMonths(-2);

            // 1. Vendor spend spike: current month vs previous month
            var vendorByMonth = await _warehouse.GetVendorSpendByMonthAsync(userId, realmId, twoMonthsAgo, now, cancellationToken);
            var currentMonthStart = periodStart;
            var previousMonthStart = periodStart.AddMonths(-1);
            var byVendor = vendorByMonth
                .GroupBy(r => r.VendorName)
                .Select(g => new
                {
                    VendorName = g.Key,
                    Current = g.Where(x => x.PeriodStart == currentMonthStart).Sum(x => x.TotalSpend),
                    Previous = g.Where(x => x.PeriodStart == previousMonthStart).Sum(x => x.TotalSpend)
                })
                .Where(x => x.Previous > 0 && x.Current > x.Previous * VendorSpikeFactor)
                .ToList();

            foreach (var v in byVendor)
            {
                await _anomalyRepo.InsertAsync(new AnomalyEvent
                {
                    UserId = userId,
                    RealmId = realmId,
                    Type = "VendorSpendSpike",
                    Severity = "Medium",
                    Details = $"Vendor '{v.VendorName}' spend this month is more than {VendorSpikeFactor * 100}% of previous month.",
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            // 2. Large single transaction (expenses and revenue)
            var expenseStats = await _warehouse.GetFactExpenseStatsAsync(userId, realmId, cancellationToken);
            if (expenseStats.Count > 1 && expenseStats.AvgAmount > 0 && expenseStats.MaxAmount > expenseStats.AvgAmount * LargeTransactionFactor)
            {
                await _anomalyRepo.InsertAsync(new AnomalyEvent
                {
                    UserId = userId,
                    RealmId = realmId,
                    Type = "LargeTransaction",
                    Severity = "Low",
                    Details = $"A single expense (${expenseStats.MaxAmount:N2}) is more than {LargeTransactionFactor}x the average expense (${expenseStats.AvgAmount:N2}).",
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            var revenueStats = await _warehouse.GetFactRevenueStatsAsync(userId, realmId, cancellationToken);
            if (revenueStats.Count > 1 && revenueStats.AvgAmount > 0 && revenueStats.MaxAmount > revenueStats.AvgAmount * LargeTransactionFactor)
            {
                await _anomalyRepo.InsertAsync(new AnomalyEvent
                {
                    UserId = userId,
                    RealmId = realmId,
                    Type = "LargeTransaction",
                    Severity = "Low",
                    Details = $"A single invoice (${revenueStats.MaxAmount:N2}) is more than {LargeTransactionFactor}x the average invoice (${revenueStats.AvgAmount:N2}).",
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            // 3. Overdue receivables
            var overdueCutoff = now.AddDays(-OverdueDays);
            var overdueCount = await _invoiceRepo.GetOverdueReceivablesCountAsync(realmId, overdueCutoff);
            if (overdueCount >= OverdueReceivablesThreshold)
            {
                await _anomalyRepo.InsertAsync(new AnomalyEvent
                {
                    UserId = userId,
                    RealmId = realmId,
                    Type = "OverdueReceivables",
                    Severity = "High",
                    Details = $"{overdueCount} invoice(s) with balance are over {OverdueDays} days past due.",
                    DetectedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }
    }
}
