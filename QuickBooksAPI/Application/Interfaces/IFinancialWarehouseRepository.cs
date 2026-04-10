using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IFinancialWarehouseRepository
{
    Task RebuildFactsAsync(int userId, string realmId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorSpendTopRow>> GetVendorSpendTopAsync(int userId, string realmId, int periodDays, int limit, CancellationToken cancellationToken = default);
    Task<VendorSpendSummaryRow> GetVendorSpendSummaryAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProfitabilityRow>> GetCustomerProfitabilityAsync(int userId, string realmId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevenueExpensesMonthlyRow>> GetRevenueExpensesMonthlyAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorSpendByMonthRow>> GetVendorSpendByMonthAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<ExpenseRevenueStatsRow> GetFactExpenseStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default);
    Task<ExpenseRevenueStatsRow> GetFactRevenueStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default);
}
