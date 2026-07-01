using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlChatRepository
{
    /// <summary>Returns all messages for a run in ascending chronological order.</summary>
    Task<IReadOnlyList<GlChatMessage>> GetHistoryAsync(int runId, CancellationToken ct = default);

    Task AddMessageAsync(GlChatMessage message, CancellationToken ct = default);

    Task ClearHistoryAsync(int runId, CancellationToken ct = default);
}
