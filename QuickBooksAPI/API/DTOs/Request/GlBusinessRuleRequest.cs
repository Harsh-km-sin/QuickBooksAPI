using System.Text.Json;

namespace QuickBooksAPI.API.DTOs.Request;

public class GlBusinessRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Validated JSON: {type:"field"|"account_threshold", ...}
    public JsonElement Condition { get; set; }
    public string Severity { get; set; } = "medium"; // critical|high|medium|low
    public bool IsActive { get; set; } = true;
}
