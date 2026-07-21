namespace QuickBooksAPI.API.DTOs.Request;

public class GlFeedbackRequest
{
    // pending | confirmed_true_positive | dismissed_false_positive
    public string Status { get; set; } = "pending";
    // unresolved | resolved | escalated
    public string ResolutionStatus { get; set; } = "unresolved";
    public string? AuditDecision { get; set; }
    public string? Comments { get; set; }
    public string? RequiredEvidence { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public string? AssignedTo { get; set; }
}
