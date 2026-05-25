namespace QuickBooksAPI.Integrations.Abstractions;

/// <summary>
/// Pulls journal entry query JSON pages from the connected accounting provider.
/// </summary>
public interface IJournalEntryAccountingGateway
{
    Task<string> FetchJournalEntriesPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default);
}
