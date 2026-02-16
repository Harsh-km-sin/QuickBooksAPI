using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.API.DTOs.Response;
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
                    TxnDate, Sparse, Adjustment, DocNumber, PrivateNote, CurrencyCode,
                    ExchangeRate, TotalAmount, HomeTotalAmount,
                    CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOJournalEntryHeader
                WHERE QBRealmId = @RealmId
                ORDER BY TxnDate DESC, LastUpdatedTime DESC";
            return await connection.QueryAsync<QBOJournalEntryHeader>(sql, new { RealmId = realmId });
        }

        public async Task<PagedResult<QBOJournalEntryHeader>> GetPagedByRealmAsync(string realmId, int page, int pageSize, string? search)
        {
            using var connection = CreateOpenConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM dbo.QBOJournalEntryHeader 
                WHERE QBRealmId = @RealmId
                AND (@Search IS NULL OR QBJournalEntryId LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { RealmId = realmId, Search = searchPattern });

            var itemsSql = @"
                SELECT JournalEntryId, QBJournalEntryId, QBRealmId, SyncToken, Domain,
                    TxnDate, Sparse, Adjustment, DocNumber, PrivateNote, CurrencyCode,
                    ExchangeRate, TotalAmount, HomeTotalAmount,
                    CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOJournalEntryHeader
                WHERE QBRealmId = @RealmId
                AND (@Search IS NULL OR QBJournalEntryId LIKE @Search)
                ORDER BY TxnDate DESC, LastUpdatedTime DESC
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<QBOJournalEntryHeader>(itemsSql, new { RealmId = realmId, Search = searchPattern, Skip = skip, PageSize = pageSize });

            return new PagedResult<QBOJournalEntryHeader> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<int> UpsertJournalEntryHeadersAsync(IEnumerable<QBOJournalEntryHeader> entries, IDbConnection connection, IDbTransaction tx)
        {
            if (entries == null || !entries.Any())
                return 0;

            // Use direct MERGE with parameters so all columns (DocNumber, CurrencyCode, etc.) are written
            // regardless of TVP definition in the database.
            const string sql = @"
                MERGE dbo.QBOJournalEntryHeader AS target
                USING (SELECT @QBJournalEntryId AS QBJournalEntryId, @QBRealmId AS QBRealmId) AS source
                ON target.QBJournalEntryId = source.QBJournalEntryId AND target.QBRealmId = source.QBRealmId
                WHEN MATCHED THEN
                    UPDATE SET
                        SyncToken = @SyncToken,
                        Domain = @Domain,
                        TxnDate = @TxnDate,
                        Sparse = @Sparse,
                        Adjustment = @Adjustment,
                        DocNumber = @DocNumber,
                        PrivateNote = @PrivateNote,
                        CurrencyCode = @CurrencyCode,
                        ExchangeRate = @ExchangeRate,
                        TotalAmount = @TotalAmount,
                        HomeTotalAmount = @HomeTotalAmount,
                        CreateTime = @CreateTime,
                        LastUpdatedTime = @LastUpdatedTime,
                        RawJson = @RawJson
                WHEN NOT MATCHED THEN
                    INSERT (QBJournalEntryId, QBRealmId, SyncToken, Domain, TxnDate, Sparse, Adjustment,
                            DocNumber, PrivateNote, CurrencyCode, ExchangeRate, TotalAmount, HomeTotalAmount,
                            CreateTime, LastUpdatedTime, RawJson)
                    VALUES (@QBJournalEntryId, @QBRealmId, @SyncToken, @Domain, @TxnDate, @Sparse, @Adjustment,
                            @DocNumber, @PrivateNote, @CurrencyCode, @ExchangeRate, @TotalAmount, @HomeTotalAmount,
                            @CreateTime, @LastUpdatedTime, @RawJson);";

            var count = 0;
            foreach (var e in entries)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@QBJournalEntryId", e.QBJournalEntryId);
                parameters.Add("@QBRealmId", e.QBRealmId);
                parameters.Add("@SyncToken", e.SyncToken);
                parameters.Add("@Domain", e.Domain);
                parameters.Add("@TxnDate", e.TxnDate);
                parameters.Add("@Sparse", e.Sparse);
                parameters.Add("@Adjustment", e.Adjustment);
                parameters.Add("@DocNumber", e.DocNumber);
                parameters.Add("@PrivateNote", e.PrivateNote);
                parameters.Add("@CurrencyCode", e.CurrencyCode);
                parameters.Add("@ExchangeRate", e.ExchangeRate);
                parameters.Add("@TotalAmount", e.TotalAmount);
                parameters.Add("@HomeTotalAmount", e.HomeTotalAmount);
                parameters.Add("@CreateTime", e.CreateTime);
                parameters.Add("@LastUpdatedTime", e.LastUpdatedTime);
                parameters.Add("@RawJson", e.RawJson);

                count += await connection.ExecuteAsync(sql, parameters, tx);
            }

            return count;
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
