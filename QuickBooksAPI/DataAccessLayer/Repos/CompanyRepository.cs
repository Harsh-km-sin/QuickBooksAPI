using Dapper;
using QuickBooksAPI.DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly string _connectionString;

        public CompanyRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
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
                                    UpdatedAtUtc
                                FROM dbo.Companies
                                WHERE UserId = @UserId
                                  AND QboRealmId = @RealmId;";

            using var connection = CreateConnection();
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
                                    UpdatedAtUtc
                                FROM dbo.Companies
                                WHERE UserId = @UserId
                                  AND IsQboConnected = 1;";

            using var connection = CreateConnection();
            return await connection.QueryAsync<Company>(
                sql,
                new { UserId = userId });
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
                                        UpdatedAtUtc      = SYSDATETIMEOFFSET()
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
                                        UpdatedAtUtc
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
                                        SYSDATETIMEOFFSET()
                                    );";

            using var connection = CreateConnection();
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
                company.DisconnectedAtUtc
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

            using var connection = CreateConnection();
            await connection.ExecuteAsync(sql, new { UserId = userId, RealmId = realmId });
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

