using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlFeedbackRepository
{
    /// <summary>
    /// Upsert: inserts on first verdict, updates in place on subsequent changes.
    /// Uses MERGE on EntryId — one feedback row per transaction entry.
    /// </summary>
    Task UpsertAsync(GlFeedback feedback, CancellationToken ct = default);

    Task<GlFeedback?> GetByEntryAsync(int entryId, CancellationToken ct = default);
}
