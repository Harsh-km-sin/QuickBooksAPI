using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos;

/// <summary>
/// Dapper-based repository responsible for building derived financial tables
/// used by the analytics layer. It assumes the underlying tables already
/// exist in the database.
/// </summary>
public class FinancialWarehouseRepository : IFinancialWarehouseRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public FinancialWarehouseRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

    public async Task RebuildFactsAsync(int userId, string realmId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        // Simple pattern for now: clear existing rows for this User/Realm
        // and rebuild from raw QuickBooks-synced tables.
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);

        var sql = FinancialWarehouseRebuildFactsSql.Build();

        await connection.ExecuteAsync(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
    }

    public async Task<IReadOnlyList<VendorSpendTopRow>> GetVendorSpendTopAsync(int userId, string realmId, int periodDays, int limit, CancellationToken cancellationToken = default)
    {
        var periodStart = DateTime.UtcNow.Date.AddDays(-periodDays);
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);
        parameters.Add("@PeriodStart", periodStart);
        parameters.Add("@Limit", Math.Max(1, Math.Min(limit, 100)));

        var sql = @"
SELECT dv.VendorName,
       SUM(fvs.TotalSpend) AS TotalSpend,
       SUM(fvs.BillCount) AS BillCount,
       MAX(fvs.LastBillDate) AS LastBillDate
FROM FactVendorSpend fvs
INNER JOIN DimVendor dv ON dv.Id = fvs.VendorDimId AND dv.UserId = fvs.UserId AND dv.RealmId = fvs.RealmId
WHERE fvs.UserId = @UserId AND fvs.RealmId = @RealmId
  AND fvs.PeriodEnd >= @PeriodStart
GROUP BY dv.Id, dv.VendorName
ORDER BY SUM(fvs.TotalSpend) DESC
OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY;
";
        var rows = await connection.QueryAsync<VendorSpendTopRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return rows?.ToList() ?? new List<VendorSpendTopRow>();
    }

    public async Task<VendorSpendSummaryRow> GetVendorSpendSummaryAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);
        parameters.Add("@From", from.Date);
        parameters.Add("@To", to.Date);

        var sql = @"
SELECT ISNULL(SUM(fvs.TotalSpend), 0) AS TotalSpend,
       COUNT(DISTINCT fvs.VendorDimId) AS VendorCount,
       ISNULL(SUM(fvs.BillCount), 0) AS BillCount
FROM FactVendorSpend fvs
WHERE fvs.UserId = @UserId AND fvs.RealmId = @RealmId
  AND fvs.PeriodStart <= @To AND fvs.PeriodEnd >= @From;
";
        var row = await connection.QuerySingleOrDefaultAsync<VendorSpendSummaryRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return row ?? new VendorSpendSummaryRow();
    }

    public async Task<IReadOnlyList<CustomerProfitabilityRow>> GetCustomerProfitabilityAsync(int userId, string realmId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);
        parameters.Add("@From", from.Date);
        parameters.Add("@To", to.Date);
        parameters.Add("@Top", Math.Max(1, Math.Min(top, 200)));

        var sql = @"
SELECT dc.CustomerName,
       SUM(fcp.Revenue) AS Revenue,
       SUM(fcp.CostOfGoods) AS CostOfGoods,
       SUM(fcp.Revenue) - SUM(fcp.CostOfGoods) AS GrossMargin,
       CASE WHEN SUM(fcp.Revenue) > 0 THEN ((SUM(fcp.Revenue) - SUM(fcp.CostOfGoods)) / SUM(fcp.Revenue)) * 100 ELSE 0 END AS MarginPct
FROM FactCustomerProfitability fcp
INNER JOIN DimCustomer dc ON dc.Id = fcp.CustomerDimId AND dc.UserId = fcp.UserId AND dc.RealmId = fcp.RealmId
WHERE fcp.UserId = @UserId AND fcp.RealmId = @RealmId
  AND fcp.PeriodStart <= @To AND fcp.PeriodEnd >= @From
GROUP BY dc.Id, dc.CustomerName
ORDER BY (SUM(fcp.Revenue) - SUM(fcp.CostOfGoods)) DESC
OFFSET 0 ROWS FETCH NEXT @Top ROWS ONLY;
";
        var rows = await connection.QueryAsync<CustomerProfitabilityRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return rows?.ToList() ?? new List<CustomerProfitabilityRow>();
    }

    public async Task<IReadOnlyList<RevenueExpensesMonthlyRow>> GetRevenueExpensesMonthlyAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);
        parameters.Add("@From", from.Date);
        parameters.Add("@To", to.Date);

        var sql = @"
SELECT DATEFROMPARTS(YEAR(fr.Date), MONTH(fr.Date), 1) AS MonthStart,
       ISNULL(SUM(fr.NetAmount), 0) AS Revenue,
       0 AS Expenses
FROM FactRevenue fr
WHERE fr.UserId = @UserId AND fr.RealmId = @RealmId
  AND fr.Date >= @From AND fr.Date <= @To
GROUP BY YEAR(fr.Date), MONTH(fr.Date)
UNION ALL
SELECT DATEFROMPARTS(YEAR(fe.Date), MONTH(fe.Date), 1) AS MonthStart,
       0 AS Revenue,
       ISNULL(SUM(fe.NetAmount), 0) AS Expenses
FROM FactExpenses fe
WHERE fe.UserId = @UserId AND fe.RealmId = @RealmId
  AND fe.Date >= @From AND fe.Date <= @To
GROUP BY YEAR(fe.Date), MONTH(fe.Date)
ORDER BY MonthStart;
";
        var raw = await connection.QueryAsync<RevenueExpensesMonthlyRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        // Collapse by month (we have separate rows for revenue and expenses)
        var byMonth = (raw ?? Enumerable.Empty<RevenueExpensesMonthlyRow>())
            .GroupBy(r => r.MonthStart)
            .Select(g => new RevenueExpensesMonthlyRow
            {
                MonthStart = g.Key,
                Revenue = g.Sum(x => x.Revenue),
                Expenses = g.Sum(x => x.Expenses)
            })
            .OrderBy(r => r.MonthStart)
            .ToList();
        return byMonth;
    }

    public async Task<IReadOnlyList<VendorSpendByMonthRow>> GetVendorSpendByMonthAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);
        parameters.Add("@From", from.Date);
        parameters.Add("@To", to.Date);

        var sql = @"
SELECT dv.VendorName, fvs.PeriodStart, SUM(fvs.TotalSpend) AS TotalSpend
FROM FactVendorSpend fvs
INNER JOIN DimVendor dv ON dv.Id = fvs.VendorDimId AND dv.UserId = fvs.UserId AND dv.RealmId = fvs.RealmId
WHERE fvs.UserId = @UserId AND fvs.RealmId = @RealmId
  AND fvs.PeriodStart >= @From AND fvs.PeriodEnd <= @To
GROUP BY dv.VendorName, fvs.PeriodStart
ORDER BY dv.VendorName, fvs.PeriodStart;
";
        var rows = await connection.QueryAsync<VendorSpendByMonthRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return rows?.ToList() ?? new List<VendorSpendByMonthRow>();
    }

    public async Task<ExpenseRevenueStatsRow> GetFactExpenseStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);

        var sql = @"
SELECT ISNULL(AVG(fe.NetAmount), 0) AS AvgAmount, ISNULL(MAX(fe.NetAmount), 0) AS MaxAmount, COUNT(1) AS [Count]
FROM FactExpenses fe
WHERE fe.UserId = @UserId AND fe.RealmId = @RealmId;
";
        var row = await connection.QuerySingleOrDefaultAsync<ExpenseRevenueStatsRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return row ?? new ExpenseRevenueStatsRow();
    }

    public async Task<ExpenseRevenueStatsRow> GetFactRevenueStatsAsync(int userId, string realmId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@RealmId", realmId);

        var sql = @"
SELECT ISNULL(AVG(fr.NetAmount), 0) AS AvgAmount, ISNULL(MAX(fr.NetAmount), 0) AS MaxAmount, COUNT(1) AS [Count]
FROM FactRevenue fr
WHERE fr.UserId = @UserId AND fr.RealmId = @RealmId;
";
        var row = await connection.QuerySingleOrDefaultAsync<ExpenseRevenueStatsRow>(_connectionFactory.CreateCommand(sql, parameters, cancellationToken));
        return row ?? new ExpenseRevenueStatsRow();
    }
}
