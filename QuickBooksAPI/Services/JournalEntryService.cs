using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services;

public partial class JournalEntryService : IJournalEntryService
{
    private readonly IRequestContext _requestContext;
    private readonly ITokenRepository _tokenRepository;
    private readonly IQuickBooksJournalEntryService _quickBooksJournalEntryService;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly IAuthService _authService;

    public JournalEntryService(
        IRequestContext requestContext,
        ITokenRepository tokenRepository,
        IQuickBooksJournalEntryService quickBooksJournalEntryService,
        IJournalEntryRepository journalEntryRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        IAuthService authService)
    {
        _requestContext = requestContext;
        _tokenRepository = tokenRepository;
        _quickBooksJournalEntryService = quickBooksJournalEntryService;
        _journalEntryRepository = journalEntryRepository;
        _qboSyncStateRepository = qboSyncStateRepository;
        _authService = authService;
    }

    public async Task<ApiResponse<PagedResult<QBOJournalEntryHeader>>> ListJournalEntriesAsync(ListQueryParams query)
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var realmId = _requestContext.RealmId;
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _journalEntryRepository.GetPagedByRealmAsync(realmId, page, pageSize, search);
        return ApiResponse<PagedResult<QBOJournalEntryHeader>>.Ok(result);
    }
}
