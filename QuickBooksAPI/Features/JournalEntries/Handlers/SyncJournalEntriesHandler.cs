using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Features.JournalEntries.Mapping;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services.Sync;
using System.Text.Json;

namespace QuickBooksAPI.Features.JournalEntries.Handlers;

public sealed class SyncJournalEntriesHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IJournalEntryAccountingGateway _journalGateway;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly IAuthService _authService;

    public SyncJournalEntriesHandler(
        IRequestContext requestContext,
        IJournalEntryAccountingGateway journalGateway,
        IJournalEntryRepository journalEntryRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        IAuthService authService)
    {
        _requestContext = requestContext;
        _journalGateway = journalGateway;
        _journalEntryRepository = journalEntryRepository;
        _qboSyncStateRepository = qboSyncStateRepository;
        _authService = authService;
    }

    public async Task<ApiResponse<int>> HandleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(_requestContext.UserId!);
            var realmId = _requestContext.RealmId!;

            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");

            var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(
                userId, realmId, QboEntityType.Manual_Journals.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            lastUpdatedAfter = QboSyncTimeHelper.NormalizeLastUpdatedAfterFromDb(lastUpdatedAfter);

            const int PageSize = 1000;
            var startPosition = 1;
            var totalSyncedCount = 0;
            var hasMore = true;
            DateTime? maxUpdatedTime = null;

            while (hasMore)
            {
                var journalEntriesJson = await _journalGateway.FetchJournalEntriesPageAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter,
                    cancellationToken);

                var journalEntryResponse = JsonSerializer.Deserialize<QuickBooksJournalEntryResponse>(
                    journalEntriesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var journalEntries = journalEntryResponse?.QueryResponse?.JournalEntry;
                if (journalEntries == null || journalEntries.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                using var conn = _journalEntryRepository.CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                try
                {
                    foreach (var je in journalEntries)
                    {
                        if (je.MetaData?.LastUpdatedTime != null)
                        {
                            var dtoLastUpdated = je.MetaData.LastUpdatedTime.Value;
                            var dtoLastUpdatedUtc = dtoLastUpdated.UtcDateTime;

                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                                maxUpdatedTime = dtoLastUpdatedUtc;
                        }

                        var header = JournalEntrySyncMapper.MapToHeader(je, realmId);
                        await _journalEntryRepository.UpsertJournalEntryHeadersAsync(new[] { header }, conn, tx);

                        var journalEntryId = await _journalEntryRepository.GetJournalEntryIdAsync(je.Id, realmId, conn, tx);

                        await _journalEntryRepository.DeleteJournalEntryLinesAsync(journalEntryId, conn, tx);

                        var lines = JournalEntrySyncMapper.MapToLines(je, journalEntryId);
                        await _journalEntryRepository.InsertJournalEntryLinesAsync(lines, conn, tx);

                        totalSyncedCount++;
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                if (journalEntries.Count < PageSize)
                    hasMore = false;
                else
                    startPosition += PageSize;
            }

            if (totalSyncedCount > 0 && maxUpdatedTime.HasValue)
            {
                var timeToStore = QboSyncTimeHelper.ClampFutureSyncTimestampUtc(maxUpdatedTime.Value);

                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Manual_Journals.ToString(),
                    timeToStore);
            }
            else if (isFirstSync)
            {
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Manual_Journals.ToString(),
                    DateTime.UtcNow);
            }

            return ApiResponse<int>.Ok(totalSyncedCount, $"Successfully synced {totalSyncedCount} journal entries.");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.Fail("Failed to sync journal entries.", new[] { ex.Message });
        }
    }
}
