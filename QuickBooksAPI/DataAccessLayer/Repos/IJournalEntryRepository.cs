using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IJournalEntryRepository
    {
        public IDbConnection CreateOpenConnection();
        public Task<int> UpsertJournalEntryHeadersAsync(IEnumerable<QBOJournalEntryHeader> entries, IDbConnection connection, IDbTransaction tx);
        public Task DeleteJournalEntryLinesAsync(long journalEntryId, IDbConnection connection, IDbTransaction tx);
        public Task<int> InsertJournalEntryLinesAsync(IEnumerable<QBOJournalEntryLine> lines, IDbConnection connection, IDbTransaction tx);
        public Task<long> GetJournalEntryIdAsync(string qbJournalEntryId, string realmId, IDbConnection conn, IDbTransaction tx);
        public Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
    }
}
