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

        public async Task<int> UpsertJournalEntryHeadersAsync(IEnumerable<QBOJournalEntryHeader> entries, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"
        MERGE QBOJournalEntryHeader AS target
        USING (VALUES
            {0}
        ) AS source
        (QBJournalEntryId, SyncToken, Domain, TxnDate, Sparse, Adjustment,
         CreateTime, LastUpdatedTime, RawJson, QBRealmId)
        ON target.QBJournalEntryId = source.QBJournalEntryId
           AND target.QBRealmId = source.QBRealmId
        WHEN MATCHED THEN
            UPDATE SET
                SyncToken = source.SyncToken,
                Domain = source.Domain,
                TxnDate = source.TxnDate,
                Sparse = source.Sparse,
                Adjustment = source.Adjustment,
                CreateTime = source.CreateTime,
                LastUpdatedTime = source.LastUpdatedTime,
                RawJson = source.RawJson
        WHEN NOT MATCHED THEN
            INSERT (
                QBJournalEntryId, SyncToken, Domain, TxnDate, Sparse, Adjustment,
                CreateTime, LastUpdatedTime, RawJson, QBRealmId
            )
            VALUES (
                source.QBJournalEntryId, source.SyncToken, source.Domain, source.TxnDate,
                source.Sparse, source.Adjustment, source.CreateTime,
                source.LastUpdatedTime, source.RawJson, source.QBRealmId
            );";

            var values = entries.Select((e, i) =>
                $"(@QBJournalEntryId{i}, @SyncToken{i}, @Domain{i}, @TxnDate{i}, @Sparse{i}, @Adjustment{i}, " +
                $"@CreateTime{i}, @LastUpdatedTime{i}, @RawJson{i}, @QBRealmId{i})");

            sql = string.Format(sql, string.Join(", ", values));

            var parameters = new DynamicParameters();
            int idx = 0;

            foreach (var e in entries)
            {
                parameters.Add($"@QBJournalEntryId{idx}", e.QBJournalEntryId);
                parameters.Add($"@SyncToken{idx}", e.SyncToken);
                parameters.Add($"@Domain{idx}", e.Domain);
                parameters.Add($"@TxnDate{idx}", e.TxnDate);
                parameters.Add($"@Sparse{idx}", e.Sparse);
                parameters.Add($"@Adjustment{idx}", e.Adjustment);
                parameters.Add($"@CreateTime{idx}", e.CreateTime);
                parameters.Add($"@LastUpdatedTime{idx}", e.LastUpdatedTime);
                parameters.Add($"@RawJson{idx}", e.RawJson);
                parameters.Add($"@QBRealmId{idx}", e.QBRealmId);
                idx++;
            }

            return await connection.ExecuteAsync(sql, parameters, tx);
        }

        public async Task DeleteJournalEntryLinesAsync(long journalEntryId, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"DELETE FROM QBOJournalEntryLine WHERE JournalEntryId = @JournalEntryId";
            await connection.ExecuteAsync(sql, new { JournalEntryId = journalEntryId }, tx);
        }

        public async Task<int> InsertJournalEntryLinesAsync(IEnumerable<QBOJournalEntryLine> lines, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"
        INSERT INTO QBOJournalEntryLine
        (
            JournalEntryId, QBLineId, LineNum, DetailType, Description,
            Amount, PostingType,
            AccountRefId, AccountRefName,
            EntityType, EntityRefId, EntityRefName,
            ProjectRefId, RawLineJson
        )
        VALUES
        {0};";

            var values = lines.Select((l, i) =>
                $"(@JournalEntryId{i}, @QBLineId{i}, @LineNum{i}, @DetailType{i}, @Description{i}, " +
                $"@Amount{i}, @PostingType{i}, @AccountRefId{i}, @AccountRefName{i}, " +
                $"@EntityType{i}, @EntityRefId{i}, @EntityRefName{i}, @ProjectRefId{i}, @RawLineJson{i})");

            sql = string.Format(sql, string.Join(", ", values));

            var parameters = new DynamicParameters();
            int idx = 0;

            foreach (var l in lines)
            {
                parameters.Add($"@JournalEntryId{idx}", l.JournalEntryId);
                parameters.Add($"@QBLineId{idx}", l.QBLineId);
                parameters.Add($"@LineNum{idx}", l.LineNum);
                parameters.Add($"@DetailType{idx}", l.DetailType);
                parameters.Add($"@Description{idx}", l.Description);
                parameters.Add($"@Amount{idx}", l.Amount);
                parameters.Add($"@PostingType{idx}", l.PostingType);
                parameters.Add($"@AccountRefId{idx}", l.AccountRefId);
                parameters.Add($"@AccountRefName{idx}", l.AccountRefName);
                parameters.Add($"@EntityType{idx}", l.EntityType);
                parameters.Add($"@EntityRefId{idx}", l.EntityRefId);
                parameters.Add($"@EntityRefName{idx}", l.EntityRefName);
                parameters.Add($"@ProjectRefId{idx}", l.ProjectRefId);
                parameters.Add($"@RawLineJson{idx}", l.RawLineJson);
                idx++;
            }

            return await connection.ExecuteAsync(sql, parameters, tx);
        }

        public async Task<long> GetJournalEntryIdAsync(string qbJournalEntryId, string realmId, IDbConnection conn, IDbTransaction tx)
        {
            var sql = @"
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
            string sql = @"
                SELECT MAX(LastUpdatedTime)
                FROM QBOJournalEntryHeader
                WHERE QBRealmId = @RealmId";

            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { RealmId = realmId });
        }
    }
}
