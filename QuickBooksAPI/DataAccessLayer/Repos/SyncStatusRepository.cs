using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class SyncStatusRepository : ISyncStatusRepository
    {
        private readonly string _connectionString;

        public SyncStatusRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> IsRunningAsync(string companyId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM CompanySyncStatus
                WHERE CompanyId = @CompanyId
                  AND Status IN ('Queued','Running')
                  AND UpdatedAt > DATEADD(MINUTE, -10, SYSUTCDATETIME())";

            using var connection = new SqlConnection(_connectionString);
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

            using var connection = new SqlConnection(_connectionString);
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

            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<SyncStatusDto>(sql, new { CompanyId = companyId });
        }
    }
}
