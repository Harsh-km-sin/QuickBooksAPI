namespace QuickBooksAPI.API.DTOs.Response
{
    public class VendorSpendDto
    {
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public int BillCount { get; set; }
        public DateTime? LastBillDate { get; set; }
        public DateTime PeriodStart { get; set; }
    }

    public class VendorSpendSummaryDto
    {
        public decimal TotalSpend { get; set; }
        public int VendorCount { get; set; }
        public int BillCount { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class CustomerProfitabilityDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal CostOfGoods { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal MarginPct { get; set; }
        public DateTime PeriodStart { get; set; }
    }

    public class CashflowDto
    {
        public DateTime Date { get; set; }
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal NetCash => CashIn - CashOut;
    }

    public class RevenueExpensesMonthlyDto
    {
        public DateTime MonthStart { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
    }

    public class AnomalyDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public class KpiSnapshotDto
    {
        public DateTime SnapshotDate { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public decimal KpiValue { get; set; }
        public string Period { get; set; } = "Monthly";
    }

    public class ForecastScenarioDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int HorizonMonths { get; set; }
        public string? AssumptionsJson { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class ForecastResultDto
    {
        public DateTime PeriodStart { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
        public decimal CashBalance { get; set; }
        public decimal? RunwayMonths { get; set; }
    }

    public class ForecastDetailDto
    {
        public ForecastScenarioDto Scenario { get; set; } = null!;
        public IReadOnlyList<ForecastResultDto> Results { get; set; } = Array.Empty<ForecastResultDto>();
    }

    public class CloseIssueDto
    {
        public int Id { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime DetectedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class ConsolidatedPnlRowDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
    }

    public class EntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public bool IsConsolidatedNode { get; set; }
    }
}
