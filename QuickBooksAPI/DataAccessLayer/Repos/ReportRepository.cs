using System.Data;
using Dapper;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ReportRepository : IReportRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ReportRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = _connectionFactory.CreateConnection();
            conn.Open();
            return conn;
        }

        public async Task UpsertReportRunAsync(
            FlattenedReport report,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var parameters = new DynamicParameters();
            parameters.Add("@Run", BuildRunTable(report.Run).AsTableValuedParameter("dbo.ReportRunUpsertType"));
            parameters.Add("@Columns", BuildColumnTable(report.Columns).AsTableValuedParameter("dbo.ReportColumnUpsertType"));
            parameters.Add("@Rows", BuildRowTable(report.Rows).AsTableValuedParameter("dbo.ReportRowUpsertType"));
            parameters.Add("@Values", BuildValueTable(report.Values).AsTableValuedParameter("dbo.ReportRowColumnValueUpsertType"));

            await connection.ExecuteAsync(
                "dbo.UpsertReportRun",
                parameters,
                transaction,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> DeleteAllRunsAsync(int userId, string realmId, string reportType)
        {
            // Child tables cascade from dbo.QBOReportRun.
            const string sql = @"
                                DELETE FROM dbo.QBOReportRun
                                WHERE UserId = @UserId
                                  AND RealmId = @RealmId
                                  AND ReportType = @ReportType;";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteAsync(sql, new { UserId = userId, RealmId = realmId, ReportType = reportType });
        }

        public async Task<IEnumerable<ReportLineRow>> GetProfitAndLossAsync(
            int userId, string realmId, DateTime rangeStart, DateTime rangeEnd, string accountingMethod)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ReportLineRow>(
                "dbo.GetProfitAndLossTree",
                new
                {
                    UserId = userId,
                    RealmId = realmId,
                    RangeStart = rangeStart,
                    RangeEnd = rangeEnd,
                    AccountingMethod = accountingMethod
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<ReportLineRow>> GetBalanceSheetAsync(
            int userId, string realmId, DateTime rangeStart, DateTime rangeEnd, string accountingMethod)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ReportLineRow>(
                "dbo.GetBalanceSheetTree",
                new
                {
                    UserId = userId,
                    RealmId = realmId,
                    RangeStart = rangeStart,
                    RangeEnd = rangeEnd,
                    AccountingMethod = accountingMethod
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<QBOReportRun>> GetSyncedRunsAsync(int userId, string realmId, string reportType)
        {
            const string sql = @"
                                SELECT
                                    Id,
                                    UserId,
                                    RealmId,
                                    ReportType,
                                    Granularity,
                                    AccountingMethod,
                                    ValueSemantics,
                                    PeriodStart,
                                    PeriodEnd,
                                    Currency,
                                    NoReportData,
                                    GeneratedAtUtc,
                                    SyncedAtUtc,
                                    CreatedAtUtc,
                                    UpdatedAtUtc
                                FROM dbo.QBOReportRun
                                WHERE UserId = @UserId
                                  AND RealmId = @RealmId
                                  AND ReportType = @ReportType
                                ORDER BY PeriodStart;";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<QBOReportRun>(
                sql,
                new { UserId = userId, RealmId = realmId, ReportType = reportType });
        }

        public async Task<DateTimeOffset?> GetLastSyncedAtAsync(int userId, string realmId, string reportType)
        {
            const string sql = @"
                                SELECT MAX(SyncedAtUtc)
                                FROM dbo.QBOReportRun
                                WHERE UserId = @UserId
                                  AND RealmId = @RealmId
                                  AND ReportType = @ReportType;";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<DateTimeOffset?>(
                sql,
                new { UserId = userId, RealmId = realmId, ReportType = reportType });
        }

        // ------------------------------------------------------------------
        // TVP builders. Column order MUST match the type definitions in
        // Scripts/UpsertReport.sql exactly - DataTable.Rows.Add(...) is positional.
        // ------------------------------------------------------------------

        private static DataTable BuildRunTable(ReportRunUpsertRow run)
        {
            var table = new DataTable();
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("RealmId", typeof(string));
            table.Columns.Add("ReportType", typeof(string));
            table.Columns.Add("Granularity", typeof(string));
            table.Columns.Add("AccountingMethod", typeof(string));
            table.Columns.Add("ValueSemantics", typeof(string));
            table.Columns.Add("PeriodStart", typeof(DateTime));
            table.Columns.Add("PeriodEnd", typeof(DateTime));
            table.Columns.Add("Currency", typeof(string));
            table.Columns.Add("NoReportData", typeof(bool));
            table.Columns.Add("GeneratedAtUtc", typeof(DateTimeOffset));
            table.Columns.Add("RawJson", typeof(string));

            table.Rows.Add(
                run.UserId,
                run.RealmId,
                run.ReportType,
                run.Granularity,
                run.AccountingMethod,
                run.ValueSemantics,
                run.PeriodStart,
                run.PeriodEnd,
                string.IsNullOrEmpty(run.Currency) ? DBNull.Value : run.Currency,
                run.NoReportData,
                run.GeneratedAtUtc ?? (object)DBNull.Value,
                string.IsNullOrEmpty(run.RawJson) ? DBNull.Value : run.RawJson);

            return table;
        }

        private static DataTable BuildColumnTable(IEnumerable<ReportColumnUpsertRow> columns)
        {
            var table = new DataTable();
            table.Columns.Add("ColumnNumber", typeof(int));
            table.Columns.Add("ColKey", typeof(string));
            table.Columns.Add("ColTitle", typeof(string));
            table.Columns.Add("ColType", typeof(string));
            table.Columns.Add("ColPeriodStart", typeof(DateTime));
            table.Columns.Add("ColPeriodEnd", typeof(DateTime));

            foreach (var column in columns)
            {
                table.Rows.Add(
                    column.ColumnNumber,
                    string.IsNullOrEmpty(column.ColKey) ? DBNull.Value : column.ColKey,
                    string.IsNullOrEmpty(column.ColTitle) ? DBNull.Value : column.ColTitle,
                    string.IsNullOrEmpty(column.ColType) ? DBNull.Value : column.ColType,
                    column.ColPeriodStart ?? (object)DBNull.Value,
                    column.ColPeriodEnd ?? (object)DBNull.Value);
            }

            return table;
        }

        private static DataTable BuildRowTable(IEnumerable<ReportRowUpsertRow> rows)
        {
            var table = new DataTable();
            table.Columns.Add("RowNumber", typeof(int));
            table.Columns.Add("ParentRowNumber", typeof(int));
            table.Columns.Add("Depth", typeof(int));
            table.Columns.Add("RowType", typeof(string));
            table.Columns.Add("GroupName", typeof(string));
            table.Columns.Add("Label", typeof(string));
            table.Columns.Add("AccountQboId", typeof(string));
            table.Columns.Add("IsSummaryRow", typeof(bool));
            table.Columns.Add("RowPath", typeof(string));
            table.Columns.Add("ParentRowPath", typeof(string));

            foreach (var row in rows)
            {
                table.Rows.Add(
                    row.RowNumber,
                    row.ParentRowNumber ?? (object)DBNull.Value,
                    row.Depth,
                    row.RowType,
                    string.IsNullOrEmpty(row.GroupName) ? DBNull.Value : row.GroupName,
                    string.IsNullOrEmpty(row.Label) ? DBNull.Value : row.Label,
                    string.IsNullOrEmpty(row.AccountQboId) ? DBNull.Value : row.AccountQboId,
                    row.IsSummaryRow,
                    row.RowPath,
                    string.IsNullOrEmpty(row.ParentRowPath) ? DBNull.Value : row.ParentRowPath);
            }

            return table;
        }

        private static DataTable BuildValueTable(IEnumerable<ReportRowColumnValueUpsertRow> values)
        {
            var table = new DataTable();
            table.Columns.Add("RowNumber", typeof(int));
            table.Columns.Add("ColumnNumber", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("RawValue", typeof(string));

            foreach (var value in values)
            {
                table.Rows.Add(
                    value.RowNumber,
                    value.ColumnNumber,
                    value.Amount ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(value.RawValue) ? DBNull.Value : value.RawValue);
            }

            return table;
        }
    }
}
