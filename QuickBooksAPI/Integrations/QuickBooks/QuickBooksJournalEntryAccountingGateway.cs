using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Integrations.QuickBooks;

internal sealed class QuickBooksJournalEntryAccountingGateway : IJournalEntryAccountingGateway
{
    private readonly IQuickBooksJournalEntryService _journalEntryService;

    public QuickBooksJournalEntryAccountingGateway(IQuickBooksJournalEntryService journalEntryService)
    {
        _journalEntryService = journalEntryService ?? throw new ArgumentNullException(nameof(journalEntryService));
    }

    public Task<string> FetchJournalEntriesPageAsync(
        string accessToken,
        string realmId,
        int startPosition,
        int pageSize,
        DateTime? incrementalSinceUtc,
        CancellationToken cancellationToken = default) =>
        _journalEntryService.GetJournalEntryAsync(accessToken, realmId, startPosition, pageSize, incrementalSinceUtc);
}
