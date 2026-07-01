namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlRun
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RealmId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Processing | Complete | Failed
    public int ProgressPercentage { get; set; }
    public string? SourceFormat { get; set; } // quickbooks | generic
    public int? TotalTransactions { get; set; }
    public int? FlaggedCount { get; set; }
    public int? CriticalCount { get; set; }
    public int? HighCount { get; set; }
    public int? MediumCount { get; set; }
    public int? LowCount { get; set; }
    public int? NormalCount { get; set; }
    public double? AvgRiskScore { get; set; }
    public decimal? MaterialExposure { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public string? AiExecutiveSummary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
