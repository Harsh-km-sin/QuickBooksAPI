namespace QuickBooksAPI.DataAccessLayer.Models;

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
