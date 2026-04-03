namespace QuickBooksAPI.Application.Interfaces;

public interface ICfoAssistantService
{
    Task<CfoAssistantResponse> AskAsync(int userId, string realmId, string question, CancellationToken cancellationToken = default);
}

public class CfoAssistantResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<CitationDto> Citations { get; set; } = new();
}

public class CitationDto
{
    public string MetricName { get; set; } = string.Empty;
    public string? DateRange { get; set; }
    public string? Endpoint { get; set; }
}
