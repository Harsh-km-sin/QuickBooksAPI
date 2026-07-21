namespace QuickBooksAPI.API.DTOs.Response;

public class GlFeedbackDto
{
    public int Id { get; set; }
    public int EntryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = string.Empty;
    public string? AuditDecision { get; set; }
    public string? Comments { get; set; }
    public string? RequiredEvidence { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AssignedTo { get; set; }
}
