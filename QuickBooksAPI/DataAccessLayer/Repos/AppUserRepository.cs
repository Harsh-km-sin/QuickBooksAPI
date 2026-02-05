using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly string _connectionString;

        public AppUserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<int> RegisterUserAsync(AppUser user)
        {
            using var connection = CreateConnection();
            var sql = @"
            INSERT INTO AppUser (FirstName, LastName, Username, Email, [Password])
            VALUES (@FirstName, @LastName, @Username, @Email, @Password);

            SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, user);
        }
        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM AppUser WHERE Email = @Email";
            return await connection.QueryFirstOrDefaultAsync<AppUser>(sql, new { Email = email });
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM AppUser WHERE Username = @Username";
            return await connection.QueryFirstOrDefaultAsync<AppUser>(sql, new { Username = username });
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT COUNT(1) FROM AppUser WHERE Id = @UserId";
            var count = await connection.QuerySingleAsync<int>(query, new { UserId = userId });
            return count > 0;
        }

    }
}
