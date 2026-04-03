using System.Text;

namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// One handler per CFO question intent; register multiple implementations in DI for parallel-safe extension.
/// </summary>
public interface ICfoAssistantIntentHandler
{
    bool Matches(string questionLowerInvariant);

    Task AppendAsync(int userId, string realmId, CfoAssistantContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mutable accumulation of CFO context lines and citations while handlers run.
/// </summary>
public sealed class CfoAssistantContext
{
    public StringBuilder Narrative { get; } = new();
    public List<CitationDto> Citations { get; } = new();
}
