using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace SyncWorker
{
    public class KpiSnapshotFunction
    {
        private readonly ILogger<KpiSnapshotFunction> _logger;
        private readonly IServiceProvider _serviceProvider;

        public KpiSnapshotFunction(ILogger<KpiSnapshotFunction> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [Function(nameof(KpiSnapshotFunction))]
        public async Task Run(
            [TimerTrigger("0 0 2 * * *", RunOnStartup = false)] TimerInfo timerInfo)
        {
            _logger.LogInformation("KPI snapshot timer triggered at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
            var warehouseRepo = scope.ServiceProvider.GetRequiredService<IFinancialWarehouseRepository>();
            var kpiRepo = scope.ServiceProvider.GetRequiredService<IKpiSnapshotRepository>();

            var companies = await companyRepo.GetDistinctConnectedUserRealmAsync();
            var snapshotDate = DateTime.UtcNow.Date;

            foreach (var (userId, realmId) in companies)
            {
                try
                {
                    await ComputeAndUpsertKpisAsync(userId, realmId, snapshotDate, warehouseRepo, kpiRepo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KPI snapshot failed for UserId={UserId}, RealmId={RealmId}", userId, realmId);
                }
            }
        }

        private static async Task ComputeAndUpsertKpisAsync(
            int userId,
            string realmId,
            DateTime snapshotDate,
            IFinancialWarehouseRepository warehouseRepo,
            IKpiSnapshotRepository kpiRepo)
        {
            var lastMonthStart = new DateTime(snapshotDate.Year, snapshotDate.Month, 1).AddMonths(-1);
            var lastMonthEnd = lastMonthStart.AddMonths(1).AddDays(-1);
            var prevMonthStart = lastMonthStart.AddMonths(-1);
            var prevMonthEnd = lastMonthStart.AddDays(-1);

            // Last two months revenue/expenses for RevenueGrowth and BurnMultiple
            var monthly = await warehouseRepo.GetRevenueExpensesMonthlyAsync(userId, realmId, prevMonthStart, lastMonthEnd, default);
            var lastMonth = monthly.FirstOrDefault(m => m.MonthStart == lastMonthStart);
            var prevMonth = monthly.FirstOrDefault(m => m.MonthStart == prevMonthStart);

            // GrossMargin from customer profitability (last month only)
            var profitability = await warehouseRepo.GetCustomerProfitabilityAsync(userId, realmId, lastMonthStart, lastMonthEnd, 1000, default);
            var totalRevenue = profitability.Sum(p => p.Revenue);
            var totalCogs = profitability.Sum(p => p.CostOfGoods);
            var grossMarginPct = totalRevenue > 0
                ? (double)((totalRevenue - totalCogs) / totalRevenue * 100)
                : 0;

            await kpiRepo.UpsertAsync(new KpiSnapshot
            {
                UserId = userId,
                RealmId = realmId,
                SnapshotDate = snapshotDate,
                KpiName = "GrossMargin",
                KpiValue = (decimal)grossMarginPct,
                Period = "Monthly"
            }, default);

            decimal revenueGrowth = 0;
            if (prevMonth != null && lastMonth != null && prevMonth.Revenue > 0)
            {
                revenueGrowth = (lastMonth.Revenue - prevMonth.Revenue) / prevMonth.Revenue * 100;
            }
            await kpiRepo.UpsertAsync(new KpiSnapshot
            {
                UserId = userId,
                RealmId = realmId,
                SnapshotDate = snapshotDate,
                KpiName = "RevenueGrowth",
                KpiValue = revenueGrowth,
                Period = "Monthly"
            }, default);

            decimal burnMultiple = 0;
            if (lastMonth != null && lastMonth.Revenue > 0 && lastMonth.Expenses > 0)
            {
                burnMultiple = lastMonth.Expenses / lastMonth.Revenue;
            }
            await kpiRepo.UpsertAsync(new KpiSnapshot
            {
                UserId = userId,
                RealmId = realmId,
                SnapshotDate = snapshotDate,
                KpiName = "BurnMultiple",
                KpiValue = burnMultiple,
                Period = "Monthly"
            }, default);
        }
    }
}
