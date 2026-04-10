using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.Application.Interfaces;

public interface IJournalEntryRepository
{
    IDbConnection CreateOpenConnection();
    Task<IEnumerable<QBOJournalEntryHeader>> GetAllByRealmAsync(string realmId);
    Task<PagedResult<QBOJournalEntryHeader>> GetPagedByRealmAsync(string realmId, int page, int pageSize, string? search);
    Task<int> UpsertJournalEntryHeadersAsync(IEnumerable<QBOJournalEntryHeader> entries, IDbConnection connection, IDbTransaction tx);
    Task DeleteJournalEntryLinesAsync(long journalEntryId, IDbConnection connection, IDbTransaction tx);
    Task<int> InsertJournalEntryLinesAsync(IEnumerable<QBOJournalEntryLine> lines, IDbConnection connection, IDbTransaction tx);
    Task<long> GetJournalEntryIdAsync(string qbJournalEntryId, string realmId, IDbConnection conn, IDbTransaction tx);
    Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
}
