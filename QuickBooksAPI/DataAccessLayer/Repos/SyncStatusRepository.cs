using Dapper;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class SyncStatusRepository : ISyncStatusRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SyncStatusRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<bool> IsRunningAsync(string companyId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM CompanySyncStatus
                WHERE CompanyId = @CompanyId
                  AND Status IN ('Queued','Running')
                  AND UpdatedAt > DATEADD(MINUTE, -10, SYSUTCDATETIME())";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { CompanyId = companyId }) > 0;
        }

        public async Task SetStatusAsync(string companyId, string status, string? error = null)
        {
            const string sql = @"
                MERGE CompanySyncStatus AS target
                USING (SELECT @CompanyId AS CompanyId) AS source
                ON target.CompanyId = source.CompanyId
                WHEN MATCHED THEN
                    UPDATE SET
                        Status = @Status,
                        LastRun = SYSUTCDATETIME(),
                        Error = @Error,
                        UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (CompanyId, Status, LastRun, Error)
                    VALUES (@CompanyId, @Status, SYSUTCDATETIME(), @Error);";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                CompanyId = companyId,
                Status = status,
                Error = error
            });
        }

        public async Task<SyncStatusDto?> GetStatusAsync(string companyId)
        {
            const string sql = @"
                SELECT CompanyId, Status, LastRun, Error
                FROM CompanySyncStatus
                WHERE CompanyId = @CompanyId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<SyncStatusDto>(sql, new { CompanyId = companyId });
        }
    }
}
