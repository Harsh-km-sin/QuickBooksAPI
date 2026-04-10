using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Linq;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class TokenRepository : ITokenRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public TokenRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task SaveTokenAsync(QuickBooksToken token)
        {
            using var connection = _connectionFactory.CreateConnection();

            // Use MERGE to support multiple companies per user (upsert on UserId + RealmId)
            var sql = @"
MERGE INTO QuickBooksToken AS target
USING (SELECT @UserId AS UserId, @RealmId AS RealmId) AS source
    ON target.UserId = source.UserId AND target.RealmId = source.RealmId
WHEN MATCHED THEN
    UPDATE SET
        IdToken = @IdToken,
        AccessToken = @AccessToken,
        RefreshToken = @RefreshToken,
        TokenType = @TokenType,
        ExpiresIn = @ExpiresIn,
        XRefreshTokenExpiresIn = @XRefreshTokenExpiresIn,
        CreatedAt = @CreatedAt,
        UpdatedAt = @UpdatedAt
WHEN NOT MATCHED THEN
    INSERT (UserId, RealmId, IdToken, AccessToken, RefreshToken, TokenType, ExpiresIn, XRefreshTokenExpiresIn, CreatedAt, UpdatedAt)
    VALUES (@UserId, @RealmId, @IdToken, @AccessToken, @RefreshToken, @TokenType, @ExpiresIn, @XRefreshTokenExpiresIn, @CreatedAt, @UpdatedAt);";

            if (token.CreatedAt == default)
                token.CreatedAt = DateTime.UtcNow;

            token.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(sql, token);
        }

        public async Task<QuickBooksToken?> GetTokenByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var query = @"SELECT * FROM QuickBooksToken 
                  WHERE UserId = @UserId AND RealmId = @RealmId";
            return await connection.QueryFirstOrDefaultAsync<QuickBooksToken>(query,
                new { UserId = userId, RealmId = realmId });
        }

        public async Task DeleteTokenAsync(int tokenId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var query = "DELETE FROM QuickBooksToken WHERE Id = @TokenId";
            await connection.ExecuteAsync(query, new { TokenId = tokenId });
        }

        public async Task<IEnumerable<string>> GetRealmIdsByUserIdAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var query = "SELECT DISTINCT RealmId FROM QuickBooksToken WHERE UserId = @UserId";
            var realmIds = await connection.QueryAsync<string>(query, new { UserId = userId });
            return realmIds ?? Enumerable.Empty<string>();
        }

        public async Task UpdateTokenAsync(QuickBooksToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                UPDATE QuickBooksToken
                SET IdToken = @IdToken,
                    AccessToken = @AccessToken,
                    RefreshToken = @RefreshToken,
                    TokenType = @TokenType,
                    ExpiresIn = @ExpiresIn,
                    XRefreshTokenExpiresIn = @XRefreshTokenExpiresIn,
                    CreatedAt = @CreatedAt,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            token.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(sql, new
            {
                token.Id,
                token.IdToken,
                token.AccessToken,
                token.RefreshToken,
                token.TokenType,
                token.ExpiresIn,
                token.XRefreshTokenExpiresIn,
                token.CreatedAt,
                token.UpdatedAt
            });
        }
    }
}
