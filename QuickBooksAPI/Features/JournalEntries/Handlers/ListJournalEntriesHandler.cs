using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.JournalEntries.Handlers;

public sealed class ListJournalEntriesHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IJournalEntryRepository _journalEntryRepository;

    public ListJournalEntriesHandler(IRequestContext requestContext, IJournalEntryRepository journalEntryRepository)
    {
        _requestContext = requestContext;
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<ApiResponse<PagedResult<QBOJournalEntryHeader>>> HandleAsync(ListQueryParams query)
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Fail(FeatureRequestContextGuard.MissingContextMessage);

        var realmId = _requestContext.RealmId;
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _journalEntryRepository.GetPagedByRealmAsync(realmId, page, pageSize, search, query.SortBy, query.IsDescending());
        return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Ok(result);
    }
}
