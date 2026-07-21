namespace QuickBooksAPI.API.DTOs.Response;

public class GlBusinessRuleDto
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object Condition { get; set; } = null!; // deserialized from JSON
    public string Severity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
