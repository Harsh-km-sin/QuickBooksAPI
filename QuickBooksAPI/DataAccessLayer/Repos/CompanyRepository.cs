using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public CompanyRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<Company?> GetByUserAndRealmAsync(int userId, string realmId)
        {
            const string sql = @"
                                SELECT
                                    Id,
                                    UserId,
                                    QboRealmId,
                                    CompanyName,
                                    QboAccessToken,
                                    QboRefreshToken,
                                    TokenExpiryUtc,
                                    IsQboConnected,
                                    ConnectedAtUtc,
                                    DisconnectedAtUtc,
                                    CreatedAtUtc,
                                    UpdatedAtUtc,
                                    AccountingBasis,
                                    CompanyStartDate,
                                    FiscalYearStartMonth
                                FROM dbo.Companies
                                WHERE UserId = @UserId
                                  AND QboRealmId = @RealmId;";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Company>(
                sql,
                new { UserId = userId, RealmId = realmId });
        }

        public async Task<IEnumerable<Company>> GetConnectedCompaniesByUserIdAsync(int userId)
        {
            const string sql = @"
                                SELECT
                                    Id,
                                    UserId,
                                    QboRealmId,
                                    CompanyName,
                                    QboAccessToken,
                                    QboRefreshToken,
                                    TokenExpiryUtc,
                                    IsQboConnected,
                                    ConnectedAtUtc,
                                    DisconnectedAtUtc,
                                    CreatedAtUtc,
                                    UpdatedAtUtc,
                                    AccountingBasis,
                                    CompanyStartDate,
                                    FiscalYearStartMonth
                                FROM dbo.Companies
                                WHERE UserId = @UserId
                                  AND IsQboConnected = 1;";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Company>(
                sql,
                new { UserId = userId });
        }

        public async Task<IEnumerable<(int UserId, string RealmId)>> GetDistinctConnectedUserRealmAsync()
        {
            const string sql = @"
                                SELECT DISTINCT UserId, QboRealmId AS RealmId
                                FROM dbo.Companies
                                WHERE IsQboConnected = 1;";
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.QueryAsync<(int UserId, string RealmId)>(sql);
            return rows;
        }

        public async Task UpsertCompanyAsync(Company company)
        {
            if (company == null) throw new ArgumentNullException(nameof(company));

            const string sql = @"
                                MERGE dbo.Companies AS target
                                USING (VALUES (@UserId, @QboRealmId)) AS source (UserId, QboRealmId)
                                    ON target.UserId = source.UserId
                                   AND target.QboRealmId = source.QboRealmId
                                WHEN MATCHED THEN
                                    UPDATE SET
                                        CompanyName       = COALESCE(@CompanyName, target.CompanyName),
                                        QboAccessToken    = @QboAccessToken,
                                        QboRefreshToken   = @QboRefreshToken,
                                        TokenExpiryUtc    = @TokenExpiryUtc,
                                        IsQboConnected    = @IsQboConnected,
                                        ConnectedAtUtc    = COALESCE(target.ConnectedAtUtc, @ConnectedAtUtc),
                                        DisconnectedAtUtc = @DisconnectedAtUtc,
                                        UpdatedAtUtc      = SYSDATETIMEOFFSET(),
                                        -- COALESCE so a failed best-effort metadata fetch never wipes good values.
                                        AccountingBasis      = COALESCE(@AccountingBasis, target.AccountingBasis),
                                        CompanyStartDate     = COALESCE(@CompanyStartDate, target.CompanyStartDate),
                                        FiscalYearStartMonth = COALESCE(@FiscalYearStartMonth, target.FiscalYearStartMonth)
                                WHEN NOT MATCHED THEN
                                    INSERT (
                                        UserId,
                                        QboRealmId,
                                        CompanyName,
                                        QboAccessToken,
                                        QboRefreshToken,
                                        TokenExpiryUtc,
                                        IsQboConnected,
                                        ConnectedAtUtc,
                                        DisconnectedAtUtc,
                                        CreatedAtUtc,
                                        UpdatedAtUtc,
                                        AccountingBasis,
                                        CompanyStartDate,
                                        FiscalYearStartMonth
                                    )
                                    VALUES (
                                        @UserId,
                                        @QboRealmId,
                                        @CompanyName,
                                        @QboAccessToken,
                                        @QboRefreshToken,
                                        @TokenExpiryUtc,
                                        @IsQboConnected,
                                        @ConnectedAtUtc,
                                        @DisconnectedAtUtc,
                                        SYSDATETIMEOFFSET(),
                                        SYSDATETIMEOFFSET(),
                                        @AccountingBasis,
                                        @CompanyStartDate,
                                        @FiscalYearStartMonth
                                    );";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                company.UserId,
                company.QboRealmId,
                company.CompanyName,
                company.QboAccessToken,
                company.QboRefreshToken,
                company.TokenExpiryUtc,
                company.IsQboConnected,
                company.ConnectedAtUtc,
                company.DisconnectedAtUtc,
                company.AccountingBasis,
                company.CompanyStartDate,
                company.FiscalYearStartMonth
            });
        }

        public async Task UpdateCompanyMetadataAsync(
            int userId,
            string realmId,
            string? accountingBasis,
            DateTime? companyStartDate,
            int? fiscalYearStartMonth)
        {
            // Touches the three metadata columns only — never the token columns.
            // COALESCE keeps a stored value when the caller could not fetch a fresh one.
            const string sql = @"
                                UPDATE dbo.Companies
                                SET
                                    AccountingBasis      = COALESCE(@AccountingBasis, AccountingBasis),
                                    CompanyStartDate     = COALESCE(@CompanyStartDate, CompanyStartDate),
                                    FiscalYearStartMonth = COALESCE(@FiscalYearStartMonth, FiscalYearStartMonth),
                                    UpdatedAtUtc         = SYSDATETIMEOFFSET()
                                WHERE UserId = @UserId
                                  AND QboRealmId = @RealmId;";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                RealmId = realmId,
                AccountingBasis = accountingBasis,
                CompanyStartDate = companyStartDate,
                FiscalYearStartMonth = fiscalYearStartMonth
            });
        }

        public async Task ClearCompanyTokenAsync(int userId, string realmId)
        {
            const string sql = @"
                                UPDATE dbo.Companies
                                SET
                                    QboAccessToken    = NULL,
                                    QboRefreshToken   = NULL,
                                    TokenExpiryUtc    = NULL,
                                    IsQboConnected    = 0,
                                    DisconnectedAtUtc = SYSDATETIMEOFFSET(),
                                    UpdatedAtUtc      = SYSDATETIMEOFFSET()
                                WHERE UserId = @UserId
                                  AND QboRealmId = @RealmId;";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new { UserId = userId, RealmId = realmId });
        }
    }
}
