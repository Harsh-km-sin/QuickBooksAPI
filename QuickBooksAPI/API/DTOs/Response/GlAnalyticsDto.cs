namespace QuickBooksAPI.API.DTOs.Response;

public class GlPeriodMetricDto
{
    public string Month { get; set; } = string.Empty; // "2024-03"
    public decimal TotalAmount { get; set; }
    public decimal FlaggedAmount { get; set; }
    public int FlaggedCount { get; set; }
    public double AvgRiskScore { get; set; }
}

public class GlEntityRiskDto
{
    public string? Party { get; set; }
    public int MaxRiskScore { get; set; }
    public int FlaggedCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class GlAnomalyBreakdownDto
{
    public string AnomalyType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
