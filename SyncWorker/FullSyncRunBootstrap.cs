using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Messages;

namespace SyncWorker;

/// <summary>
/// Per-run setup for <see cref="FullSyncOrchestrator"/> (sync context, user id parsing, completion payload).
/// </summary>
internal static class FullSyncRunBootstrap
{
    public static void InitializeSyncContext(SyncContext syncContext, FullSyncMessage data)
    {
        syncContext.UserId = data.UserId;
        syncContext.RealmId = data.CompanyId;
        syncContext.CorrelationId = string.IsNullOrWhiteSpace(data.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : data.CorrelationId.Trim();
    }

    /// <summary>Returns 0 when <paramref name="userId"/> is not an integer (QBO per-entity sync state is skipped).</summary>
    public static int TryParseUserId(string userId, ILogger logger)
    {
        if (!int.TryParse(userId, out var id))
        {
            logger.LogWarning(
                "Full sync: UserId could not be parsed as int ({UserId}). QBO Sync State will not be updated.",
                userId);
            return 0;
        }

        return id;
    }

    public static FullSyncCompletionContext CreateCompletionContext(
        FullSyncMessage data,
        SyncContext syncContext,
        bool succeeded,
        IReadOnlyDictionary<string, int> results,
        IReadOnlyList<string> errors) =>
        new(
            data.CompanyId,
            data.UserId,
            syncContext.CorrelationId ?? string.Empty,
            succeeded,
            results,
            errors);
}
