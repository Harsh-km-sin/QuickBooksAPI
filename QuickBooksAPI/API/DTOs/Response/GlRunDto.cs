namespace QuickBooksAPI.API.DTOs.Response;

public class GlRunDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? SourceFormat { get; set; }
    public int? TotalTransactions { get; set; }
    public int? FlaggedCount { get; set; }
    public int? CriticalCount { get; set; }
    public int? HighCount { get; set; }
    public int? MediumCount { get; set; }
    public int? LowCount { get; set; }
    public int? NormalCount { get; set; }
    public double? AvgRiskScore { get; set; }
    public decimal? MaterialExposure { get; set; }
    public string? PeriodStart { get; set; }
    public string? PeriodEnd { get; set; }
    public string? AiExecutiveSummary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GlRunSummaryDto
{
    public GlRunDto Run { get; set; } = null!;
    public Dictionary<string, int> RiskDistribution { get; set; } = new();
    public List<GlTransactionDto> TopFlagged { get; set; } = new();
}

public class GlUploadResponseDto
{
    public int RunId { get; set; }
    public string Status { get; set; } = "Pending";
}
