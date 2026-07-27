using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class TermRepository : ITermRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public TermRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private IDbConnection CreateOpenConnection()
        {
            var conn = _connectionFactory.CreateConnection();
            conn.Open();
            return conn;
        }

        public async Task<IEnumerable<QBOTerm>> GetAllByRealmAsync(string realmId, bool activeOnly = true)
        {
            using var connection = CreateOpenConnection();
            EnsureTableExists(connection);

            var sql = activeOnly
                ? @"SELECT TermId, QBOTermId, RealmId, SyncToken, Name, Active, Type, DiscountPercent, DiscountDays, DueDays, DayOfMonthDue, DueNextMonthDays, CreateTime, LastUpdatedTime, RawJson
                    FROM dbo.QBOTerm
                    WHERE RealmId = @RealmId AND Active = 1
                    ORDER BY Name ASC"
                : @"SELECT TermId, QBOTermId, RealmId, SyncToken, Name, Active, Type, DiscountPercent, DiscountDays, DueDays, DayOfMonthDue, DueNextMonthDays, CreateTime, LastUpdatedTime, RawJson
                    FROM dbo.QBOTerm
                    WHERE RealmId = @RealmId
                    ORDER BY Name ASC";

            return await connection.QueryAsync<QBOTerm>(sql, new { RealmId = realmId });
        }

        public async Task<QBOTerm?> GetByQbIdAsync(string qboTermId, string realmId)
        {
            using var connection = CreateOpenConnection();
            EnsureTableExists(connection);

            const string sql = @"
                SELECT TermId, QBOTermId, RealmId, SyncToken, Name, Active, Type, DiscountPercent, DiscountDays, DueDays, DayOfMonthDue, DueNextMonthDays, CreateTime, LastUpdatedTime, RawJson
                FROM dbo.QBOTerm
                WHERE QBOTermId = @QBOTermId AND RealmId = @RealmId";

            return await connection.QueryFirstOrDefaultAsync<QBOTerm>(sql, new { QBOTermId = qboTermId, RealmId = realmId });
        }

        public async Task UpsertTermsAsync(IEnumerable<QBOTerm> terms)
        {
            if (terms == null || !terms.Any()) return;

            using var connection = CreateOpenConnection();
            EnsureTableExists(connection);

            const string sql = @"
                MERGE dbo.QBOTerm AS target
                USING (VALUES
                    (@QBOTermId, @RealmId, @SyncToken, @Name, @Active, @Type, @DiscountPercent, @DiscountDays, @DueDays, @DayOfMonthDue, @DueNextMonthDays, @CreateTime, @LastUpdatedTime, @RawJson)
                ) AS source (
                    QBOTermId, RealmId, SyncToken, Name, Active, Type, DiscountPercent, DiscountDays, DueDays, DayOfMonthDue, DueNextMonthDays, CreateTime, LastUpdatedTime, RawJson
                )
                ON target.QBOTermId = source.QBOTermId AND target.RealmId = source.RealmId
                WHEN MATCHED THEN
                    UPDATE SET
                        SyncToken = source.SyncToken,
                        Name = source.Name,
                        Active = source.Active,
                        Type = source.Type,
                        DiscountPercent = source.DiscountPercent,
                        DiscountDays = source.DiscountDays,
                        DueDays = source.DueDays,
                        DayOfMonthDue = source.DayOfMonthDue,
                        DueNextMonthDays = source.DueNextMonthDays,
                        CreateTime = source.CreateTime,
                        LastUpdatedTime = source.LastUpdatedTime,
                        RawJson = source.RawJson
                WHEN NOT MATCHED THEN
                    INSERT (QBOTermId, RealmId, SyncToken, Name, Active, Type, DiscountPercent, DiscountDays, DueDays, DayOfMonthDue, DueNextMonthDays, CreateTime, LastUpdatedTime, RawJson)
                    VALUES (source.QBOTermId, source.RealmId, source.SyncToken, source.Name, source.Active, source.Type, source.DiscountPercent, source.DiscountDays, source.DueDays, source.DayOfMonthDue, source.DueNextMonthDays, source.CreateTime, source.LastUpdatedTime, source.RawJson);";

            foreach (var term in terms)
            {
                await connection.ExecuteAsync(sql, term);
            }
        }

        private static void EnsureTableExists(IDbConnection connection)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.QBOInvoiceHeader') AND name = 'CustomerMemo')
                BEGIN
                    ALTER TABLE dbo.QBOInvoiceHeader ADD CustomerMemo NVARCHAR(MAX) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.QBOInvoiceHeader') AND name = 'SalesTermRefId')
                BEGIN
                    ALTER TABLE dbo.QBOInvoiceHeader ADD SalesTermRefId NVARCHAR(50) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.QBOTerm') AND type in (N'U'))
                BEGIN
                    CREATE TABLE dbo.QBOTerm (
                        TermId BIGINT IDENTITY(1,1) PRIMARY KEY,
                        QBOTermId NVARCHAR(50) NOT NULL,
                        RealmId NVARCHAR(50) NOT NULL,
                        SyncToken NVARCHAR(50) NULL,
                        Name NVARCHAR(100) NOT NULL,
                        Active BIT NOT NULL DEFAULT 1,
                        Type NVARCHAR(50) NULL,
                        DiscountPercent DECIMAL(5,2) NULL,
                        DiscountDays INT NULL,
                        DueDays INT NULL,
                        DayOfMonthDue INT NULL,
                        DueNextMonthDays INT NULL,
                        CreateTime DATETIMEOFFSET NULL,
                        LastUpdatedTime DATETIMEOFFSET NULL,
                        RawJson NVARCHAR(MAX) NULL,
                        CONSTRAINT UQ_QBOTerm_QB_Realm UNIQUE (QBOTermId, RealmId)
                    );
                END";
            try
            {
                connection.Execute(sql);
            }
            catch
            {
                // Table auto-creation guard
            }
        }
    }
}
