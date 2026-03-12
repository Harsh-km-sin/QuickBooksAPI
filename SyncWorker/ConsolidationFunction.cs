using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace SyncWorker
{
    public class ConsolidationFunction
    {
        private readonly ILogger<ConsolidationFunction> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ConsolidationFunction(ILogger<ConsolidationFunction> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [Function(nameof(ConsolidationFunction))]
        public async Task Run(
            [TimerTrigger("0 0 4 1 * *", RunOnStartup = false)] TimerInfo timerInfo)
        {
            _logger.LogInformation("Consolidation timer triggered at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
            var dimEntityRepo = scope.ServiceProvider.GetRequiredService<IDimEntityRepository>();
            var warehouseRepo = scope.ServiceProvider.GetRequiredService<IFinancialWarehouseRepository>();
            var consolidatedPnlRepo = scope.ServiceProvider.GetRequiredService<IConsolidatedPnlRepository>();

            var companies = await companyRepo.GetDistinctConnectedUserRealmAsync();
            var userIds = companies.Select(c => c.UserId).Distinct().ToList();

            var toDate = DateTime.UtcNow.Date;
            var fromDate = toDate.AddMonths(-12);
            var fxRate = 1.0m;

            foreach (var userId in userIds)
            {
                try
                {
                    var parents = await dimEntityRepo.GetParentEntitiesAsync(userId, default);
                    foreach (var parent in parents)
                    {
                        var children = await dimEntityRepo.GetChildrenAsync(parent.Id, default);
                        if (children.Count == 0)
                            continue;

                        var revenueByMonth = new Dictionary<DateTime, decimal>();
                        var expensesByMonth = new Dictionary<DateTime, decimal>();

                        foreach (var child in children)
                        {
                            var rows = await warehouseRepo.GetRevenueExpensesMonthlyAsync(userId, child.RealmId, fromDate, toDate, default);
                            foreach (var r in rows)
                            {
                                var monthStart = r.MonthStart.Date;
                                if (!revenueByMonth.ContainsKey(monthStart)) revenueByMonth[monthStart] = 0;
                                if (!expensesByMonth.ContainsKey(monthStart)) expensesByMonth[monthStart] = 0;
                                revenueByMonth[monthStart] += r.Revenue * fxRate;
                                expensesByMonth[monthStart] += r.Expenses * fxRate;
                            }
                        }

                        foreach (var kv in revenueByMonth)
                        {
                            var periodStart = kv.Key;
                            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
                            var revenue = kv.Value;
                            var expenses = expensesByMonth.GetValueOrDefault(periodStart, 0);
                            await consolidatedPnlRepo.UpsertAsync(new FactConsolidatedPnl
                            {
                                EntityId = parent.Id,
                                PeriodStart = periodStart,
                                PeriodEnd = periodEnd,
                                Revenue = revenue,
                                Expenses = expenses,
                                NetIncome = revenue - expenses,
                                FxRateApplied = fxRate
                            }, default);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consolidation failed for UserId={UserId}", userId);
                }
            }
        }
    }
}
