namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlBusinessRule
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    // JSON: {type:"field"|"account_threshold", field, operator, value} or account threshold schema
    public string Condition { get; set; } = string.Empty;
    // critical | high | medium | low
    public string Severity { get; set; } = "medium";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
