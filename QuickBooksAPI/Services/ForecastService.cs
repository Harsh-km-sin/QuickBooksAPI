using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using System.Text.Json;

namespace QuickBooksAPI.Services
{
    public class ForecastAssumptions
    {
        public decimal RevenueMultiplier { get; set; } = 1.0m;
        public decimal ExpenseMultiplier { get; set; } = 1.0m;
    }

    public interface IForecastService
    {
        Task<int> CreateAndComputeAsync(int userId, string realmId, string name, int horizonMonths, string? assumptionsJson, string? createdBy, CancellationToken cancellationToken = default);
        Task<ForecastDetailDto?> GetForecastAsync(int scenarioId, int userId, string realmId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Creates forecast scenarios and computes deterministic projections from warehouse data.
    /// </summary>
    public class ForecastService : IForecastService
    {
        private readonly IForecastScenarioRepository _scenarioRepo;
        private readonly IForecastResultRepository _resultRepo;
        private readonly IFinancialWarehouseRepository _warehouse;
        private readonly ICashRunwayService _runwayService;

        public ForecastService(
            IForecastScenarioRepository scenarioRepo,
            IForecastResultRepository resultRepo,
            IFinancialWarehouseRepository warehouse,
            ICashRunwayService runwayService)
        {
            _scenarioRepo = scenarioRepo;
            _resultRepo = resultRepo;
            _warehouse = warehouse;
            _runwayService = runwayService;
        }

        public async Task<int> CreateAndComputeAsync(int userId, string realmId, string name, int horizonMonths, string? assumptionsJson, string? createdBy, CancellationToken cancellationToken = default)
        {
            var scenario = new ForecastScenario
            {
                UserId = userId,
                RealmId = realmId,
                Name = name,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = createdBy,
                HorizonMonths = Math.Clamp(horizonMonths, 1, 60),
                AssumptionsJson = assumptionsJson,
                Status = "Pending"
            };
            var id = await _scenarioRepo.InsertAsync(scenario, cancellationToken);

            try
            {
                var assumptions = ParseAssumptions(assumptionsJson);
                var runway = await _runwayService.GetRunwayAsync(userId, realmId, cancellationToken);
                var periodEnd = DateTime.UtcNow.Date;
                var periodStart = periodEnd.AddMonths(-12);
                var monthly = await _warehouse.GetRevenueExpensesMonthlyAsync(userId, realmId, periodStart, periodEnd, cancellationToken);
                var ordered = monthly.OrderBy(m => m.MonthStart).ToList();
                var lastThree = ordered.TakeLast(3).ToList();
                var avgRevenue = lastThree.Count > 0 ? lastThree.Average(m => m.Revenue) : 0m;
                var avgExpenses = lastThree.Count > 0 ? lastThree.Average(m => m.Expenses) : runway.MonthlyBurn > 0 ? runway.MonthlyBurn : 0m;
                if (avgExpenses <= 0 && runway.MonthlyBurn > 0) avgExpenses = runway.MonthlyBurn;

                var monthlyBurn = avgExpenses > 0 ? avgExpenses : 1m;
                var cash = runway.CurrentCash;
                var results = new List<ForecastResult>();
                var startMonth = new DateTime(periodEnd.Year, periodEnd.Month, 1).AddMonths(1);

                for (var i = 0; i < scenario.HorizonMonths; i++)
                {
                    var periodStartDate = startMonth.AddMonths(i);
                    var revenue = decimal.Round(avgRevenue * assumptions.RevenueMultiplier, 2);
                    var expenses = decimal.Round(avgExpenses * assumptions.ExpenseMultiplier, 2);
                    var netIncome = revenue - expenses;
                    cash += netIncome;
                    if (cash < 0) cash = 0;
                    var runwayMonths = monthlyBurn > 0 ? cash / monthlyBurn : (decimal?)null;

                    results.Add(new ForecastResult
                    {
                        ScenarioId = id,
                        PeriodStart = periodStartDate,
                        Revenue = revenue,
                        Expenses = expenses,
                        NetIncome = netIncome,
                        CashBalance = decimal.Round(cash, 2),
                        RunwayMonths = runwayMonths.HasValue ? decimal.Round(runwayMonths.Value, 1) : null
                    });
                }

                await _resultRepo.InsertBatchAsync(results, cancellationToken);
                await _scenarioRepo.UpdateStatusAsync(id, "Completed", cancellationToken);
            }
            catch
            {
                await _scenarioRepo.UpdateStatusAsync(id, "Failed", cancellationToken);
                throw;
            }

            return id;
        }

        public async Task<ForecastDetailDto?> GetForecastAsync(int scenarioId, int userId, string realmId, CancellationToken cancellationToken = default)
        {
            var scenario = await _scenarioRepo.GetByIdAndUserRealmAsync(scenarioId, userId, realmId, cancellationToken);
            if (scenario == null) return null;

            var results = await _resultRepo.GetByScenarioIdAsync(scenarioId, cancellationToken);
            return new ForecastDetailDto
            {
                Scenario = new ForecastScenarioDto
                {
                    Id = scenario.Id,
                    Name = scenario.Name,
                    CreatedAtUtc = scenario.CreatedAtUtc,
                    CreatedBy = scenario.CreatedBy,
                    HorizonMonths = scenario.HorizonMonths,
                    AssumptionsJson = scenario.AssumptionsJson,
                    Status = scenario.Status
                },
                Results = results.Select(r => new ForecastResultDto
                {
                    PeriodStart = r.PeriodStart,
                    Revenue = r.Revenue,
                    Expenses = r.Expenses,
                    NetIncome = r.NetIncome,
                    CashBalance = r.CashBalance,
                    RunwayMonths = r.RunwayMonths
                }).ToList()
            };
        }

        private static ForecastAssumptions ParseAssumptions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new ForecastAssumptions();
            try
            {
                var parsed = JsonSerializer.Deserialize<ForecastAssumptions>(json);
                return parsed ?? new ForecastAssumptions();
            }
            catch
            {
                return new ForecastAssumptions();
            }
        }
    }
}
