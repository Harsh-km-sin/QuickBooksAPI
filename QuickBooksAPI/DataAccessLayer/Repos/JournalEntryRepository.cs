using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;
using System.Linq;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class JournalEntryRepository : IJournalEntryRepository
    {
        private readonly string _connectionString;

        public JournalEntryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task<IEnumerable<QBOJournalEntryHeader>> GetAllByRealmAsync(string realmId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"
                SELECT JournalEntryId, QBJournalEntryId, QBRealmId, SyncToken, Domain,
                    TxnDate, Sparse, Adjustment, CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOJournalEntryHeader
                WHERE QBRealmId = @RealmId
                ORDER BY TxnDate DESC, LastUpdatedTime DESC";
            return await connection.QueryAsync<QBOJournalEntryHeader>(sql, new { RealmId = realmId });
        }

        public async Task<int> UpsertJournalEntryHeadersAsync(IEnumerable<QBOJournalEntryHeader> entries, IDbConnection connection, IDbTransaction tx)
        {
            if (entries == null || !entries.Any())
                return 0;

            var headersTable = BuildJournalEntryHeaderTable(entries);
            var parameters = new DynamicParameters();
            parameters.Add("@Headers", headersTable.AsTableValuedParameter("dbo.JournalEntryHeaderUpsertType"));

            return await connection.ExecuteAsync("dbo.UpsertJournalEntryHeader", parameters, tx, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteJournalEntryLinesAsync(long journalEntryId, IDbConnection connection, IDbTransaction tx)
        {
            const string sql = @"DELETE FROM QBOJournalEntryLine WHERE JournalEntryId = @JournalEntryId";
            await connection.ExecuteAsync(sql, new { JournalEntryId = journalEntryId }, tx);
        }

        public async Task<int> InsertJournalEntryLinesAsync(IEnumerable<QBOJournalEntryLine> lines, IDbConnection connection, IDbTransaction tx)
        {
            if (lines == null || !lines.Any())
                return 0;

            var linesTable = BuildJournalEntryLineTable(lines);
            var parameters = new DynamicParameters();
            parameters.Add("@Lines", linesTable.AsTableValuedParameter("dbo.JournalEntryLineInsertType"));

            return await connection.ExecuteAsync("dbo.InsertJournalEntryLines", parameters, tx, commandType: CommandType.StoredProcedure);
        }

        public async Task<long> GetJournalEntryIdAsync(string qbJournalEntryId, string realmId, IDbConnection conn, IDbTransaction tx)
        {
            const string sql = @"
                SELECT JournalEntryId
                FROM QBOJournalEntryHeader
                WHERE QBJournalEntryId = @QBJournalEntryId
                  AND QBRealmId = @QBRealmId";

            return await conn.ExecuteScalarAsync<long>(
                sql,
                new { QBJournalEntryId = qbJournalEntryId, QBRealmId = realmId },
                tx);
        }

        public async Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"
                SELECT MAX(LastUpdatedTime)
                FROM QBOJournalEntryHeader
                WHERE QBRealmId = @RealmId";

            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { RealmId = realmId });
        }

        private static DataTable BuildJournalEntryHeaderTable(IEnumerable<QBOJournalEntryHeader> entries)
        {
            var table = new DataTable();
            table.Columns.Add("QBJournalEntryId", typeof(string));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("Domain", typeof(string));
            table.Columns.Add("TxnDate", typeof(DateTime));
            table.Columns.Add("Sparse", typeof(bool));
            table.Columns.Add("Adjustment", typeof(bool));
            table.Columns.Add("CreateTime", typeof(DateTimeOffset));
            table.Columns.Add("LastUpdatedTime", typeof(DateTimeOffset));
            table.Columns.Add("RawJson", typeof(string));
            table.Columns.Add("QBRealmId", typeof(string));

            foreach (var e in entries)
            {
                table.Rows.Add(
                    e.QBJournalEntryId,
                    string.IsNullOrEmpty(e.SyncToken) ? DBNull.Value : e.SyncToken,
                    string.IsNullOrEmpty(e.Domain) ? DBNull.Value : e.Domain,
                    e.TxnDate ?? (object)DBNull.Value,
                    e.Sparse ?? (object)DBNull.Value,
                    e.Adjustment ?? (object)DBNull.Value,
                    e.CreateTime ?? (object)DBNull.Value,
                    e.LastUpdatedTime ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(e.RawJson) ? DBNull.Value : e.RawJson,
                    e.QBRealmId);
            }
            return table;
        }

        private static DataTable BuildJournalEntryLineTable(IEnumerable<QBOJournalEntryLine> lines)
        {
            var table = new DataTable();
            table.Columns.Add("JournalEntryId", typeof(long));
            table.Columns.Add("QBLineId", typeof(string));
            table.Columns.Add("LineNum", typeof(int));
            table.Columns.Add("DetailType", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("PostingType", typeof(string));
            table.Columns.Add("AccountRefId", typeof(string));
            table.Columns.Add("AccountRefName", typeof(string));
            table.Columns.Add("EntityType", typeof(string));
            table.Columns.Add("EntityRefId", typeof(string));
            table.Columns.Add("EntityRefName", typeof(string));
            table.Columns.Add("ProjectRefId", typeof(string));
            table.Columns.Add("RawLineJson", typeof(string));

            foreach (var l in lines)
            {
                table.Rows.Add(
                    l.JournalEntryId,
                    string.IsNullOrEmpty(l.QBLineId) ? DBNull.Value : l.QBLineId,
                    l.LineNum ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(l.DetailType) ? DBNull.Value : l.DetailType,
                    string.IsNullOrEmpty(l.Description) ? DBNull.Value : l.Description,
                    l.Amount,
                    string.IsNullOrEmpty(l.PostingType) ? DBNull.Value : l.PostingType,
                    string.IsNullOrEmpty(l.AccountRefId) ? DBNull.Value : l.AccountRefId,
                    string.IsNullOrEmpty(l.AccountRefName) ? DBNull.Value : l.AccountRefName,
                    string.IsNullOrEmpty(l.EntityType) ? DBNull.Value : l.EntityType,
                    string.IsNullOrEmpty(l.EntityRefId) ? DBNull.Value : l.EntityRefId,
                    string.IsNullOrEmpty(l.EntityRefName) ? DBNull.Value : l.EntityRefName,
                    string.IsNullOrEmpty(l.ProjectRefId) ? DBNull.Value : l.ProjectRefId,
                    string.IsNullOrEmpty(l.RawLineJson) ? DBNull.Value : l.RawLineJson);
            }
            return table;
        }
    }
}
