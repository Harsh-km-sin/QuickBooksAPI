using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public record CashRunwayResult(
        decimal CurrentCash,
        decimal MonthlyBurn,
        decimal ExpectedRevenue,
        decimal RunwayMonths);

    public interface ICashRunwayService
    {
        Task<CashRunwayResult> GetRunwayAsync(int userId, string realmId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Computes a simple, explainable cash runway using chart of accounts and
    /// warehouse facts. This is intentionally conservative and transparent.
    /// </summary>
    public class CashRunwayService : ICashRunwayService
    {
        private readonly IChartOfAccountsRepository _chartRepo;

        public CashRunwayService(IChartOfAccountsRepository chartRepo)
        {
            _chartRepo = chartRepo;
        }

        public async Task<CashRunwayResult> GetRunwayAsync(int userId, string realmId, CancellationToken cancellationToken = default)
        {
            // Current cash: sum of bank/cash accounts current balance
            var accounts = await _chartRepo.GetAllByUserAndRealmAsync(userId, realmId);
            var cashAccounts = accounts
                .Where(a =>
                    string.Equals(a.AccountType, "Bank", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.AccountType, "Other Current Asset", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.AccountType, "Cash", StringComparison.OrdinalIgnoreCase));

            var currentCash = cashAccounts.Sum(a => a.CurrentBalanceWithSubAccounts != 0m
                ? a.CurrentBalanceWithSubAccounts
                : a.CurrentBalance);

            // For phase 1, approximate burn and expected revenue;
            // these can later be backed directly by warehouse facts.
            // Use simple heuristics so the feature is available immediately.
            var monthlyBurn = Math.Max(0m, accounts
                .Where(a => string.Equals(a.Classification, "Expense", StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.CurrentBalance));

            var expectedRevenue = Math.Max(0m, accounts
                .Where(a => string.Equals(a.Classification, "Income", StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.CurrentBalance));

            var burnForRunway = monthlyBurn <= 0 ? 1m : monthlyBurn;
            var runwayMonths = currentCash / burnForRunway;

            return new CashRunwayResult(
                CurrentCash: decimal.Round(currentCash, 2),
                MonthlyBurn: decimal.Round(monthlyBurn, 2),
                ExpectedRevenue: decimal.Round(expectedRevenue, 2),
                RunwayMonths: decimal.Round(runwayMonths, 1));
        }
    }
}

