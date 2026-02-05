using Dapper;
using QuickBooksAPI.DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class TokenRepository : ITokenRepository
    {
        private readonly string _connectionString;

        public TokenRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task SaveTokenAsync(QuickBooksToken token)
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO QuickBooksToken
                (UserId, RealmId, IdToken, AccessToken, RefreshToken, TokenType, ExpiresIn, XRefreshTokenExpiresIn, CreatedAt, UpdatedAt)
                VALUES
                (@UserId, @RealmId, @IdToken, @AccessToken, @RefreshToken, @TokenType, @ExpiresIn, @XRefreshTokenExpiresIn, @CreatedAt, @UpdatedAt)";

            if (token.CreatedAt == default)
                token.CreatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(sql, token);
        }

        public async Task<QuickBooksToken?> GetTokenByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT * FROM QuickBooksToken 
                  WHERE UserId = @UserId AND RealmId = @RealmId";
            return await connection.QueryFirstOrDefaultAsync<QuickBooksToken>(query,
                new { UserId = userId, RealmId = realmId });
        }

        public async Task DeleteTokenAsync(int tokenId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "DELETE FROM QuickBooksToken WHERE Id = @TokenId";
            await connection.ExecuteAsync(query, new { TokenId = tokenId });
        }

        public async Task<IEnumerable<string>> GetRealmIdsByUserIdAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT DISTINCT RealmId FROM QuickBooksToken WHERE UserId = @UserId";
            var realmIds = await connection.QueryAsync<string>(query, new { UserId = userId });
            return realmIds ?? Enumerable.Empty<string>();
        }

        public async Task UpdateTokenAsync(QuickBooksToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            using var connection = CreateConnection();
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
