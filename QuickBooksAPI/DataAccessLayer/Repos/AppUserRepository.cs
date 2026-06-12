using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public AppUserRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

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

        public async Task<AppUser?> GetByIdAsync(int userId)
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<AppUser>(
                "SELECT * FROM AppUser WHERE Id = @UserId", new { UserId = userId });
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            using var connection = CreateConnection();
            var query = "SELECT COUNT(1) FROM AppUser WHERE Id = @UserId";
            var count = await connection.QuerySingleAsync<int>(query, new { UserId = userId });
            return count > 0;
        }

        public async Task UpdateProfileAsync(int userId, string firstName, string lastName, string username)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(
                "UPDATE AppUser SET FirstName = @FirstName, LastName = @LastName, Username = @Username WHERE Id = @UserId",
                new { UserId = userId, FirstName = firstName, LastName = lastName, Username = username });
        }

        public async Task UpdatePasswordAsync(int userId, string hashedPassword)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(
                "UPDATE AppUser SET [Password] = @Password WHERE Id = @UserId",
                new { UserId = userId, Password = hashedPassword });
        }

    }
}
