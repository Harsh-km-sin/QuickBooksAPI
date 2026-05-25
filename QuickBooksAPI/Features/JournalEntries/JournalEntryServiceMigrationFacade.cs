using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Features.JournalEntries.Handlers;

namespace QuickBooksAPI.Features.JournalEntries;

/// <summary>MIGRATION-ONLY: <see cref="IJournalEntryService"/> via feature handlers.</summary>
public sealed class JournalEntryServiceMigrationFacade : IJournalEntryService
{
    private readonly ListJournalEntriesHandler _list;
    private readonly SyncJournalEntriesHandler _sync;

    public JournalEntryServiceMigrationFacade(ListJournalEntriesHandler list, SyncJournalEntriesHandler sync)
    {
        _list = list;
        _sync = sync;
    }

    public Task<ApiResponse<PagedResult<QBOJournalEntryHeader>>> ListJournalEntriesAsync(ListQueryParams query) =>
        _list.HandleAsync(query);

    public Task<ApiResponse<int>> SyncJournalEntriesAsync() => _sync.HandleAsync();
}
