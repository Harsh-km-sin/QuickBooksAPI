using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface IRevenueExpensesService
    {
        Task<IReadOnlyList<RevenueExpensesMonthlyDto>> GetMonthlyAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Exposes revenue vs expenses from the financial warehouse for CFO dashboard charts.
    /// </summary>
    public class RevenueExpensesService : IRevenueExpensesService
    {
        private readonly IFinancialWarehouseRepository _warehouse;

        public RevenueExpensesService(IFinancialWarehouseRepository warehouse)
        {
            _warehouse = warehouse;
        }

        public async Task<IReadOnlyList<RevenueExpensesMonthlyDto>> GetMonthlyAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var rows = await _warehouse.GetRevenueExpensesMonthlyAsync(userId, realmId, from, to, cancellationToken);
            return rows.Select(r => new RevenueExpensesMonthlyDto
            {
                MonthStart = r.MonthStart,
                Revenue = r.Revenue,
                Expenses = r.Expenses
            }).ToList();
        }
    }
}
