namespace QuickBooksAPI.Services.Sync;

/// <summary>
/// Shared UTC / sync-state timestamp rules for incremental QBO entity sync (products, journals, etc.).
/// </summary>
internal static class QboSyncTimeHelper
{
    /// <summary>
    /// Ensures <see cref="DateTimeKind.Utc"/> for LastUpdatedAfter read from DB before passing to QBO query.
    /// </summary>
    public static DateTime? NormalizeLastUpdatedAfterFromDb(DateTime? lastUpdatedAfter)
    {
        if (!lastUpdatedAfter.HasValue)
            return null;

        if (lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
            return DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);

        return lastUpdatedAfter;
    }

    /// <summary>
    /// Avoids storing a sync watermark in the future (timezone / clock skew safeguard).
    /// </summary>
    public static DateTime ClampFutureSyncTimestampUtc(DateTime utcFromRecords)
    {
        var nowUtc = DateTime.UtcNow;
        return utcFromRecords > nowUtc.AddSeconds(30) ? nowUtc : utcFromRecords;
    }
}
