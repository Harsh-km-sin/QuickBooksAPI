using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    /// <summary>
    /// Row returned when querying top vendors by spend (aggregated over a period).
    /// </summary>
    public class VendorSpendTopRow
    {
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public int BillCount { get; set; }
        public DateTime? LastBillDate { get; set; }
    }

    /// <summary>
    /// Summary aggregates for vendor spend over a date range.
    /// </summary>
    public class VendorSpendSummaryRow
    {
        public decimal TotalSpend { get; set; }
        public int VendorCount { get; set; }
        public int BillCount { get; set; }
    }

    /// <summary>
    /// Row returned when querying customer profitability (aggregated over a period).
    /// </summary>
    public class CustomerProfitabilityRow
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal CostOfGoods { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal MarginPct { get; set; }
    }

    /// <summary>
    /// Monthly revenue and expenses for revenue-vs-expenses charts.
    /// </summary>
    public class RevenueExpensesMonthlyRow
    {
        public DateTime MonthStart { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
    }

    /// <summary>
    /// Per-vendor per-month spend for anomaly detection (vendor spend spike).
    /// </summary>
    public class VendorSpendByMonthRow
    {
        public string VendorName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public decimal TotalSpend { get; set; }
    }

    /// <summary>
    /// Avg/Max amounts for anomaly detection (large single transaction).
    /// </summary>
    public class ExpenseRevenueStatsRow
    {
        public decimal AvgAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int Count { get; set; }
    }

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

    /// <summary>
    /// Dapper-based repository responsible for building derived financial tables
    /// used by the analytics layer. It assumes the underlying tables already
    /// exist in the database.
    /// </summary>
    public class FinancialWarehouseRepository : IFinancialWarehouseRepository
    {
        private readonly string _connectionString;

        public FinancialWarehouseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task RebuildFactsAsync(int userId, string realmId, CancellationToken cancellationToken = default)
        {
            using var connection = CreateConnection();

            // Simple pattern for now: clear existing rows for this User/Realm
            // and rebuild from raw QuickBooks-synced tables.
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@RealmId", realmId);

            // NOTE: These statements rely on the warehouse tables and raw QBO tables
            // following the naming conventions already in the database.
            // They intentionally avoid complex joins and CTEs to keep things readable.

            var sql = @"
DELETE FROM FactCustomerProfitability WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactVendorSpend WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactExpenses WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactRevenue WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimCustomer WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimVendor WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimAccount WHERE UserId = @UserId AND RealmId = @RealmId;

-- Dimensions
INSERT INTO DimCustomer (UserId, RealmId, CustomerQboId, CustomerName)
SELECT DISTINCT @UserId, @RealmId, QboId, COALESCE(DisplayName, CompanyName, GivenName + ' ' + FamilyName)
FROM Customer
WHERE UserId = @UserId AND RealmId = @RealmId;

INSERT INTO DimVendor (UserId, RealmId, VendorQboId, VendorName)
SELECT DISTINCT @UserId, @RealmId, QboId, COALESCE(DisplayName, CompanyName)
FROM Vendor
WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL);

INSERT INTO DimAccount (UserId, RealmId, AccountQboId, AccountName, AccountType, Classification)
SELECT DISTINCT @UserId, @RealmId, QboId, Name, AccountType, Classification
FROM ChartOfAccounts
WHERE UserId = @UserId AND RealmId = @RealmId;

-- Revenue facts from invoice headers (one row per invoice)
INSERT INTO FactRevenue (UserId, RealmId, Date, CustomerDimId, AccountDimId, InvoiceQboId, Amount, TaxAmount, NetAmount)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    CAST(h.TxnDate AS date) AS [Date],
    dc.Id AS CustomerDimId,
    NULL AS AccountDimId,
    h.QBOInvoiceId,
    h.TotalAmt AS Amount,
    0 AS TaxAmount,
    h.TotalAmt AS NetAmount
FROM QBOInvoiceHeader h
LEFT JOIN DimCustomer dc
    ON dc.UserId = @UserId AND dc.RealmId = @RealmId AND dc.CustomerQboId = h.CustomerRefId
WHERE h.RealmId = @RealmId;

-- Expense facts from bill headers (one row per bill)
INSERT INTO FactExpenses (UserId, RealmId, Date, VendorDimId, AccountDimId, BillQboId, Amount, TaxAmount, NetAmount)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    CAST(h.TxnDate AS date) AS [Date],
    dv.Id AS VendorDimId,
    NULL AS AccountDimId,
    h.QBOBillId,
    h.TotalAmt AS Amount,
    0 AS TaxAmount,
    h.TotalAmt AS NetAmount
FROM QBOBillHeader h
LEFT JOIN DimVendor dv
    ON dv.UserId = @UserId AND dv.RealmId = @RealmId AND dv.VendorQboId = h.VendorRefValue
WHERE h.RealmId = @RealmId AND (h.IsDeleted = 0 OR h.IsDeleted IS NULL);

-- Vendor spend by month
INSERT INTO FactVendorSpend (UserId, RealmId, VendorDimId, PeriodStart, PeriodEnd, TotalSpend, BillCount, LastBillDate)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    fe.VendorDimId,
    DATEFROMPARTS(YEAR(fe.Date), MONTH(fe.Date), 1) AS PeriodStart,
    EOMONTH(fe.Date) AS PeriodEnd,
    SUM(fe.NetAmount) AS TotalSpend,
    COUNT(DISTINCT fe.BillQboId) AS BillCount,
    MAX(fe.Date) AS LastBillDate
FROM FactExpenses fe
WHERE fe.UserId = @UserId AND fe.RealmId = @RealmId AND fe.VendorDimId IS NOT NULL
GROUP BY fe.VendorDimId, YEAR(fe.Date), MONTH(fe.Date);

-- Customer profitability by month (revenue minus a simple proportional COGS proxy)
INSERT INTO FactCustomerProfitability (UserId, RealmId, CustomerDimId, PeriodStart, PeriodEnd, Revenue, CostOfGoods)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    fr.CustomerDimId,
    DATEFROMPARTS(YEAR(fr.Date), MONTH(fr.Date), 1) AS PeriodStart,
    EOMONTH(fr.Date) AS PeriodEnd,
    SUM(fr.NetAmount) AS Revenue,
    SUM(fr.NetAmount) * 0.4 AS CostOfGoods -- simple 40% COGS assumption for now
FROM FactRevenue fr
WHERE fr.UserId = @UserId AND fr.RealmId = @RealmId AND fr.CustomerDimId IS NOT NULL
GROUP BY fr.CustomerDimId, YEAR(fr.Date), MONTH(fr.Date);
";

            await connection.ExecuteAsync(sql, parameters);
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
            var rows = await connection.QueryAsync<VendorSpendTopRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var row = await connection.QuerySingleOrDefaultAsync<VendorSpendSummaryRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var rows = await connection.QueryAsync<CustomerProfitabilityRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var raw = await connection.QueryAsync<RevenueExpensesMonthlyRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var rows = await connection.QueryAsync<VendorSpendByMonthRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var row = await connection.QuerySingleOrDefaultAsync<ExpenseRevenueStatsRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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
            var row = await connection.QuerySingleOrDefaultAsync<ExpenseRevenueStatsRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
            return row ?? new ExpenseRevenueStatsRow();
        }
    }
}

