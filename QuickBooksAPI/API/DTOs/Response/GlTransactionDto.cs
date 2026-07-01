namespace QuickBooksAPI.API.DTOs.Response;

public class GlTransactionDto
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? PostingType { get; set; }
    public decimal Amount { get; set; }
    public string? EntityName { get; set; }
    public string? Description { get; set; }
    public string? SourceType { get; set; }
    public string? CreatedBy { get; set; }
    public string? JournalEntryId { get; set; }
    // Risk scoring
    public int? RiskScore { get; set; }
    public double? CompositeRiskScore { get; set; }
    public string? RiskTier { get; set; }
    public List<string> AnomalyFlags { get; set; } = new();
    public double? ZScore { get; set; }
    public string? AiExplanation { get; set; }
    public object? RiskExplanation { get; set; }
    // Auditor workflow
    public string Status { get; set; } = "pending";
    public bool IsReviewed { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    // Populated on demand
    public List<GlAnomalyDto>? AnomalyDetails { get; set; }
    public GlFeedbackDto? Feedback { get; set; }
}

public class GlAnomalyDto
{
    public string AnomalyType { get; set; } = string.Empty;
    public double DetectorScore { get; set; }
    public List<string> RiskReasons { get; set; } = new();
    public object? Metadata { get; set; }
}
