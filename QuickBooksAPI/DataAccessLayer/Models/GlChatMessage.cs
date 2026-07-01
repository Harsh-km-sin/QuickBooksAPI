namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlChatMessage
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty; // user | assistant | system
    public string Content { get; set; } = string.Empty;
    public string? Metadata { get; set; } // JSON: {model, inputTokens, outputTokens}
    public DateTime CreatedAt { get; set; }
}
