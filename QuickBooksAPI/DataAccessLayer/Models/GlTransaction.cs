namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlTransaction
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public int UserId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? PostingType { get; set; }
    public decimal Amount { get; set; }
    public string? EntityName { get; set; }
    public string? Description { get; set; }
    public string? SourceType { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? JournalEntryId { get; set; }
    // Risk scoring — computed by Python worker
    public int? RiskScore { get; set; }           // 0-100 integer (display score)
    public double? CompositeRiskScore { get; set; } // 0.0-1.0 float (internal)
    public string? RiskTier { get; set; }          // Normal|Low|Medium|High|Critical
    public string? AnomalyFlags { get; set; }      // JSON string[]
    public double? ZScore { get; set; }
    // LLM output
    public string? AiExplanation { get; set; }    // plain-English ≤500 chars
    public string? RiskExplanation { get; set; }  // structured JSON from LLM
    // Auditor workflow
    public string Status { get; set; } = "pending"; // pending | reviewed | flagged
    public bool IsReviewed { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
