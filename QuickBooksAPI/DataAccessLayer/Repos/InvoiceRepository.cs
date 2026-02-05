using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;
using System.Linq;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly string _connectionString;

        public InvoiceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task UpsertInvoicesAsync(IEnumerable<QBOInvoiceHeader> headers, IEnumerable<InvoiceLineUpsertRow> lines, IDbConnection connection, IDbTransaction tx)
        {
            var headersTable = BuildInvoiceHeaderTable(headers);
            var linesTable = BuildInvoiceLineTable(lines);
            var parameters = new DynamicParameters();
            parameters.Add("@Headers", headersTable.AsTableValuedParameter("dbo.InvoiceHeaderUpsertType"));
            parameters.Add("@Lines", linesTable.AsTableValuedParameter("dbo.InvoiceLineUpsertType"));
            await connection.ExecuteAsync("dbo.UpsertInvoice", parameters, tx, commandType: CommandType.StoredProcedure);
        }

        private static DataTable BuildInvoiceHeaderTable(IEnumerable<QBOInvoiceHeader> headers)
        {
            var table = new DataTable();
            table.Columns.Add("QBOInvoiceId", typeof(string));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("Domain", typeof(string));
            table.Columns.Add("Sparse", typeof(bool));
            table.Columns.Add("TxnDate", typeof(DateTime));
            table.Columns.Add("DueDate", typeof(DateTime));
            table.Columns.Add("CustomerRefId", typeof(string));
            table.Columns.Add("CustomerRefName", typeof(string));
            table.Columns.Add("CurrencyCode", typeof(string));
            table.Columns.Add("ExchangeRate", typeof(decimal));
            table.Columns.Add("TotalAmt", typeof(decimal));
            table.Columns.Add("Balance", typeof(decimal));
            table.Columns.Add("CreateTime", typeof(DateTimeOffset));
            table.Columns.Add("LastUpdatedTime", typeof(DateTimeOffset));
            table.Columns.Add("RawJson", typeof(string));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var h in headers)
            {
                table.Rows.Add(
                    h.QBOInvoiceId,
                    h.SyncToken ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(h.Domain) ? DBNull.Value : h.Domain,
                    h.Sparse,
                    h.TxnDate,
                    h.DueDate,
                    string.IsNullOrEmpty(h.CustomerRefId) ? DBNull.Value : h.CustomerRefId,
                    string.IsNullOrEmpty(h.CustomerRefName) ? DBNull.Value : h.CustomerRefName,
                    string.IsNullOrEmpty(h.CurrencyCode) ? DBNull.Value : h.CurrencyCode,
                    h.ExchangeRate,
                    h.TotalAmt,
                    h.Balance,
                    h.CreateTime,
                    h.LastUpdatedTime,
                    string.IsNullOrEmpty(h.RawJson) ? DBNull.Value : h.RawJson,
                    h.RealmId);
            }
            return table;
        }

        private static DataTable BuildInvoiceLineTable(IEnumerable<InvoiceLineUpsertRow> lines)
        {
            var table = new DataTable();
            table.Columns.Add("QBOInvoiceId", typeof(string));
            table.Columns.Add("RealmId", typeof(string));
            table.Columns.Add("QBLineId", typeof(string));
            table.Columns.Add("LineNum", typeof(int));
            table.Columns.Add("DetailType", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("ItemRefId", typeof(string));
            table.Columns.Add("ItemRefName", typeof(string));
            table.Columns.Add("Qty", typeof(decimal));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("TaxCodeRef", typeof(string));
            table.Columns.Add("RawLineJson", typeof(string));

            foreach (var l in lines)
            {
                table.Rows.Add(
                    l.QBOInvoiceId,
                    l.RealmId,
                    l.QBLineId ?? (object)DBNull.Value,
                    l.LineNum.HasValue ? l.LineNum.Value : (object)DBNull.Value,
                    l.DetailType ?? (object)DBNull.Value,
                    l.Description ?? (object)DBNull.Value,
                    l.Amount,
                    l.ItemRefId ?? (object)DBNull.Value,
                    l.ItemRefName ?? (object)DBNull.Value,
                    l.Qty ?? (object)DBNull.Value,
                    l.UnitPrice ?? (object)DBNull.Value,
                    l.TaxCodeRef ?? (object)DBNull.Value,
                    l.RawLineJson ?? (object)DBNull.Value);
            }
            return table;
        }

        public async Task<int> UpsertInvoiceHeadersAsync(IEnumerable<QBOInvoiceHeader> invoices, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"
            MERGE QBOInvoiceHeader AS target
            USING (VALUES
                {0}
            ) AS source
            (
                QBOInvoiceId, SyncToken, Domain, Sparse,
                TxnDate, DueDate,
                CustomerRefId, CustomerRefName,
                CurrencyCode, ExchangeRate,
                TotalAmt, Balance,
                CreateTime, LastUpdatedTime,
                RawJson, RealmId
            )
            ON target.QBOInvoiceId = source.QBOInvoiceId
               AND target.RealmId = source.RealmId
            WHEN MATCHED THEN
            UPDATE SET
                SyncToken = source.SyncToken,
                Domain = source.Domain,
                Sparse = source.Sparse,
                TxnDate = source.TxnDate,
                DueDate = source.DueDate,
                CustomerRefId = source.CustomerRefId,
                CustomerRefName = source.CustomerRefName,
                CurrencyCode = source.CurrencyCode,
                ExchangeRate = source.ExchangeRate,
                TotalAmt = source.TotalAmt,
                Balance = source.Balance,
                CreateTime = source.CreateTime,
                LastUpdatedTime = source.LastUpdatedTime,
                RawJson = source.RawJson
            WHEN NOT MATCHED THEN
                INSERT (
                    QBOInvoiceId, SyncToken, Domain, Sparse,
                    TxnDate, DueDate,
                    CustomerRefId, CustomerRefName,
                    CurrencyCode, ExchangeRate,
                    TotalAmt, Balance,
                    CreateTime, LastUpdatedTime,
                    RawJson, RealmId
                )
            VALUES (
                source.QBOInvoiceId, source.SyncToken, source.Domain, source.Sparse,
                source.TxnDate, source.DueDate,
                source.CustomerRefId, source.CustomerRefName,
                source.CurrencyCode, source.ExchangeRate,
                source.TotalAmt, source.Balance,
                source.CreateTime, source.LastUpdatedTime,
                source.RawJson, source.RealmId
            );";

            var values = invoices.Select((i, idx) =>
                $"(@QBOInvoiceId{idx}, @SyncToken{idx}, @Domain{idx}, @Sparse{idx}, " +
                $"@TxnDate{idx}, @DueDate{idx}, " +
                $"@CustomerRefId{idx}, @CustomerRefName{idx}, " +
                $"@CurrencyCode{idx}, @ExchangeRate{idx}, " +
                $"@TotalAmt{idx}, @Balance{idx}, " +
                $"@CreateTime{idx}, @LastUpdatedTime{idx}, " +
                $"@RawJson{idx}, @RealmId{idx})");

            sql = string.Format(sql, string.Join(", ", values));

            var parameters = new DynamicParameters();
            int i = 0;

            foreach (var inv in invoices)
            {
                parameters.Add($"@QBOInvoiceId{i}", inv.QBOInvoiceId);
                parameters.Add($"@SyncToken{i}", inv.SyncToken);
                parameters.Add($"@Domain{i}", inv.Domain);
                parameters.Add($"@Sparse{i}", inv.Sparse);
                parameters.Add($"@TxnDate{i}", inv.TxnDate);
                parameters.Add($"@DueDate{i}", inv.DueDate);
                parameters.Add($"@CustomerRefId{i}", inv.CustomerRefId);
                parameters.Add($"@CustomerRefName{i}", inv.CustomerRefName);
                parameters.Add($"@CurrencyCode{i}", inv.CurrencyCode);
                parameters.Add($"@ExchangeRate{i}", inv.ExchangeRate);
                parameters.Add($"@TotalAmt{i}", inv.TotalAmt);
                parameters.Add($"@Balance{i}", inv.Balance);
                parameters.Add($"@CreateTime{i}", inv.CreateTime);
                parameters.Add($"@LastUpdatedTime{i}", inv.LastUpdatedTime);
                parameters.Add($"@RawJson{i}", inv.RawJson);
                parameters.Add($"@RealmId{i}", inv.RealmId);
                i++;
            }

            return await connection.ExecuteAsync(sql, parameters, tx);
        }

        public async Task DeleteInvoiceLinesAsync(long invoiceId, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"DELETE FROM QBOInvoiceLine WHERE InvoiceId = @InvoiceId";
            await connection.ExecuteAsync(sql, new { InvoiceId = invoiceId }, tx);
        }

        public async Task<int> InsertInvoiceLinesAsync(IEnumerable<QBOInvoiceLine> lines, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"
            INSERT INTO QBOInvoiceLine
            (
                InvoiceId, QBLineId, LineNum, DetailType,
                Description, Amount,
                ItemRefId, ItemRefName,
                Qty, UnitPrice,
                TaxCodeRef,
                RawLineJson
            )
            VALUES
            {0};";

            var values = lines.Select((l, i) =>
                $"(@InvoiceId{i}, @QBLineId{i}, @LineNum{i}, @DetailType{i}, " +
                $"@Description{i}, @Amount{i}, " +
                $"@ItemRefId{i}, @ItemRefName{i}, " +
                $"@Qty{i}, @UnitPrice{i}, @TaxCodeRef{i}, @RawLineJson{i})");

            sql = string.Format(sql, string.Join(", ", values));

            var parameters = new DynamicParameters();
            int idx = 0;

            foreach (var l in lines)
            {
                parameters.Add($"@InvoiceId{idx}", l.InvoiceId);
                parameters.Add($"@QBLineId{idx}", l.QBLineId);
                parameters.Add($"@LineNum{idx}", l.LineNum);
                parameters.Add($"@DetailType{idx}", l.DetailType);
                parameters.Add($"@Description{idx}", l.Description);
                parameters.Add($"@Amount{idx}", l.Amount);
                parameters.Add($"@ItemRefId{idx}", l.ItemRefId);
                parameters.Add($"@ItemRefName{idx}", l.ItemRefName);
                parameters.Add($"@Qty{idx}", l.Qty);
                parameters.Add($"@UnitPrice{idx}", l.UnitPrice);
                parameters.Add($"@TaxCodeRef{idx}", l.TaxCodeRef);
                parameters.Add($"@RawLineJson{idx}", l.RawLineJson);
                idx++;
            }

            return await connection.ExecuteAsync(sql, parameters, tx);
        }

        public async Task<long> GetInvoiceIdAsync(string qbInvoiceId, string realmId, IDbConnection connection, IDbTransaction tx)
        {
            var sql = @"
            SELECT InvoiceId
            FROM QBOInvoiceHeader
            WHERE QBOInvoiceId = @QBOInvoiceId
              AND RealmId = @RealmId";

            return await connection.ExecuteScalarAsync<long>(
                sql,
                new { QBOInvoiceId = qbInvoiceId, RealmId = realmId },
                tx);
        }
    }
}
