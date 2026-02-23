using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class BillRepository : IBillRepository
    {
        private readonly string _connectionString;

        public BillRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
        public async Task UpsertBillsAsync(IEnumerable<QBOBillHeader> headers, IEnumerable<BillLineUpsertRow> lines, IDbConnection connection, IDbTransaction tx)
        {
            var headersTable = BuildBillHeaderTable(headers);
            var linesTable = BuildBillLineTable(lines);
            var parameters = new DynamicParameters();
            parameters.Add("@Headers", headersTable.AsTableValuedParameter("dbo.BillHeaderUpsertType"));
            parameters.Add("@Lines", linesTable.AsTableValuedParameter("dbo.BillLineUpsertType"));
            await connection.ExecuteAsync("dbo.UpsertBill", parameters, tx, commandType: CommandType.StoredProcedure);
        }
        public async Task<bool> SoftDeleteBillAsync(string realmId, string qboBillId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"UPDATE dbo.QBOBillHeader SET IsDeleted = 1 WHERE RealmId = @RealmId AND QBOBillId = @QBOBillId";
            var rows = await connection.ExecuteAsync(sql, new { RealmId = realmId, QBOBillId = qboBillId });
            return rows > 0;
        }

        public async Task<IEnumerable<QBOBillHeader>> GetAllByRealmAsync(string realmId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"SELECT BillId, QBOBillId, RealmId, SyncToken, Domain, Sparse,
                APAccountRefValue, APAccountRefName, VendorRefValue, VendorRefName,
                TxnDate, DueDate, TotalAmt, Balance, IsDeleted,
                CurrencyRefValue, CurrencyRefName, SalesTermRefValue,
                CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOBillHeader WHERE RealmId = @RealmId AND (IsDeleted = 0 OR IsDeleted IS NULL)
                ORDER BY TxnDate DESC, LastUpdatedTime DESC";
            return await connection.QueryAsync<QBOBillHeader>(sql, new { RealmId = realmId });
        }

        public async Task<PagedResult<QBOBillHeader>> GetPagedByRealmAsync(string realmId, int page, int pageSize, string? search)
        {
            using var connection = CreateOpenConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM dbo.QBOBillHeader 
                WHERE RealmId = @RealmId AND (IsDeleted = 0 OR IsDeleted IS NULL)
                AND (@Search IS NULL OR VendorRefName LIKE @Search OR QBOBillId LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { RealmId = realmId, Search = searchPattern });

            var itemsSql = @"SELECT BillId, QBOBillId, RealmId, SyncToken, Domain, Sparse,
                APAccountRefValue, APAccountRefName, VendorRefValue, VendorRefName,
                TxnDate, DueDate, TotalAmt, Balance, IsDeleted,
                CurrencyRefValue, CurrencyRefName, SalesTermRefValue,
                CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOBillHeader 
                WHERE RealmId = @RealmId AND (IsDeleted = 0 OR IsDeleted IS NULL)
                AND (@Search IS NULL OR VendorRefName LIKE @Search OR QBOBillId LIKE @Search)
                ORDER BY TxnDate DESC, LastUpdatedTime DESC
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<QBOBillHeader>(itemsSql, new { RealmId = realmId, Search = searchPattern, Skip = skip, PageSize = pageSize });

            return new PagedResult<QBOBillHeader> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<QBOBillHeader?> GetByQboBillIdAsync(string realmId, string qboBillId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"SELECT BillId, QBOBillId, RealmId, SyncToken, Domain, Sparse,
                APAccountRefValue, APAccountRefName, VendorRefValue, VendorRefName,
                TxnDate, DueDate, TotalAmt, Balance, IsDeleted,
                CurrencyRefValue, CurrencyRefName, SalesTermRefValue,
                CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOBillHeader
                WHERE RealmId = @RealmId AND QBOBillId = @QBOBillId AND (IsDeleted = 0 OR IsDeleted IS NULL)";
            return await connection.QuerySingleOrDefaultAsync<QBOBillHeader>(sql, new { RealmId = realmId, QBOBillId = qboBillId });
        }

        private static DataTable BuildBillHeaderTable(IEnumerable<QBOBillHeader> headers)
        {
            var table = new DataTable();
            table.Columns.Add("QBOBillId", typeof(string));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("Domain", typeof(string));
            table.Columns.Add("Sparse", typeof(bool));
            table.Columns.Add("APAccountRefValue", typeof(string));
            table.Columns.Add("APAccountRefName", typeof(string));
            table.Columns.Add("VendorRefValue", typeof(string));
            table.Columns.Add("VendorRefName", typeof(string));
            table.Columns.Add("TxnDate", typeof(DateTime));
            table.Columns.Add("DueDate", typeof(DateTime));
            table.Columns.Add("TotalAmt", typeof(decimal));
            table.Columns.Add("Balance", typeof(decimal));
            table.Columns.Add("CurrencyRefValue", typeof(string));
            table.Columns.Add("CurrencyRefName", typeof(string));
            table.Columns.Add("SalesTermRefValue", typeof(string));
            table.Columns.Add("CreateTime", typeof(DateTimeOffset));
            table.Columns.Add("LastUpdatedTime", typeof(DateTimeOffset));
            table.Columns.Add("RawJson", typeof(string));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var h in headers)
            {
                table.Rows.Add(
                    h.QBOBillId,
                    h.SyncToken ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(h.Domain) ? DBNull.Value : h.Domain,
                    h.Sparse,
                    string.IsNullOrEmpty(h.APAccountRefValue) ? DBNull.Value : h.APAccountRefValue,
                    string.IsNullOrEmpty(h.APAccountRefName) ? DBNull.Value : h.APAccountRefName,
                    string.IsNullOrEmpty(h.VendorRefValue) ? DBNull.Value : h.VendorRefValue,
                    string.IsNullOrEmpty(h.VendorRefName) ? DBNull.Value : h.VendorRefName,
                    h.TxnDate ?? (object)DBNull.Value,
                    h.DueDate ?? (object)DBNull.Value,
                    h.TotalAmt,
                    h.Balance,
                    string.IsNullOrEmpty(h.CurrencyRefValue) ? DBNull.Value : h.CurrencyRefValue,
                    string.IsNullOrEmpty(h.CurrencyRefName) ? DBNull.Value : h.CurrencyRefName,
                    string.IsNullOrEmpty(h.SalesTermRefValue) ? DBNull.Value : h.SalesTermRefValue,
                    h.CreateTime,
                    h.LastUpdatedTime,
                    string.IsNullOrEmpty(h.RawJson) ? DBNull.Value : h.RawJson,
                    h.RealmId);
            }
            return table;
        }
        private static DataTable BuildBillLineTable(IEnumerable<BillLineUpsertRow> lines)
        {
            var table = new DataTable();
            table.Columns.Add("QBOBillId", typeof(string));
            table.Columns.Add("RealmId", typeof(string));
            table.Columns.Add("QBLineId", typeof(string));
            table.Columns.Add("LineNum", typeof(int));
            table.Columns.Add("DetailType", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("ProjectRefValue", typeof(string));
            table.Columns.Add("AccountRefValue", typeof(string));
            table.Columns.Add("AccountRefName", typeof(string));
            table.Columns.Add("TaxCodeRefValue", typeof(string));
            table.Columns.Add("BillableStatus", typeof(string));
            table.Columns.Add("CustomerRefValue", typeof(string));
            table.Columns.Add("CustomerRefName", typeof(string));
            table.Columns.Add("ItemRefValue", typeof(string));
            table.Columns.Add("ItemRefName", typeof(string));
            table.Columns.Add("Qty", typeof(decimal));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("RawLineJson", typeof(string));

            foreach (var l in lines)
            {
                table.Rows.Add(
                    l.QBOBillId,
                    l.RealmId,
                    l.QBLineId ?? (object)DBNull.Value,
                    l.LineNum.HasValue ? l.LineNum.Value : (object)DBNull.Value,
                    l.DetailType ?? (object)DBNull.Value,
                    l.Description ?? (object)DBNull.Value,
                    l.Amount,
                    l.ProjectRefValue ?? (object)DBNull.Value,
                    l.AccountRefValue ?? (object)DBNull.Value,
                    l.AccountRefName ?? (object)DBNull.Value,
                    l.TaxCodeRefValue ?? (object)DBNull.Value,
                    l.BillableStatus ?? (object)DBNull.Value,
                    l.CustomerRefValue ?? (object)DBNull.Value,
                    l.CustomerRefName ?? (object)DBNull.Value,
                    l.ItemRefValue ?? (object)DBNull.Value,
                    l.ItemRefName ?? (object)DBNull.Value,
                    l.Qty ?? (object)DBNull.Value,
                    l.UnitPrice ?? (object)DBNull.Value,
                    l.RawLineJson ?? (object)DBNull.Value);
            }
            return table;
        }
    }
}
