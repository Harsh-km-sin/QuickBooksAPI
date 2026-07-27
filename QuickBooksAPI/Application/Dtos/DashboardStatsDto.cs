namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Single-row result from <c>dbo.GetDashboardStats</c>.
/// Property names match the SQL column aliases exactly so Dapper maps them automatically.
/// </summary>
public sealed class DashboardStatsDto
{
    // -- P&L (YTD, Accrual) --
    public decimal? TotalIncome { get; set; }
    public decimal? TotalExpenses { get; set; }
    public decimal? NetIncome { get; set; }
    public DateTime? PnlRangeStart { get; set; }
    public DateTime? PnlRangeEnd { get; set; }
    public string? PnlAccountingMethod { get; set; }

    // -- Balance Sheet (latest snapshot) --
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public DateTime? BsAsOfDate { get; set; }
    public string? BsAccountingMethod { get; set; }

    // -- Entity counts --
    public int CustomersCount { get; set; }
    public int VendorsCount { get; set; }
    public int ProductsCount { get; set; }
    public int InvoicesCount { get; set; }
    public int BillsCount { get; set; }
}
