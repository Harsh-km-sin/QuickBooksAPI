namespace QuickBooksAPI.API.DTOs.Request;

public class GlChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? AiModel { get; set; } // optional override; falls back to GL_Settings.LlmModel
}
