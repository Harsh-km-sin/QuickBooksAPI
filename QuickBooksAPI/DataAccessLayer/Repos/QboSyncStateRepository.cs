using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class QboSyncStateRepository : IQboSyncStateRepository
    {
        private readonly string _connectionString;

        public QboSyncStateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task<DateTime?> GetLastUpdatedAfterAsync(int userId, string realmId, string entityType)
        {
            var sql = @"
                SELECT LastUpdatedAfter
                FROM QBO_Sync_State
                WHERE UserId = @UserId
                  AND RealmId = @RealmId
                  AND EntityType = @EntityType";

            using (var connection = CreateOpenConnection())
            {
                return await connection.ExecuteScalarAsync<DateTime?>(
                    sql,
                    new { UserId = userId, RealmId = realmId, EntityType = entityType });
            }
        }

        public async Task UpdateLastUpdatedAfterAsync(int userId, string realmId, string entityType, DateTime lastUpdatedAfter)
        {
            var sql = @"
                MERGE QBO_Sync_State AS target
                USING (VALUES (@UserId, @RealmId, @EntityType, @LastUpdatedAfter, @UpdatedAt)) 
                    AS source (UserId, RealmId, EntityType, LastUpdatedAfter, UpdatedAt)
                ON target.UserId = source.UserId 
                   AND target.RealmId = source.RealmId 
                   AND target.EntityType = source.EntityType
                WHEN MATCHED THEN
                    UPDATE SET
                        LastUpdatedAfter = source.LastUpdatedAfter,
                        UpdatedAt = source.UpdatedAt
                WHEN NOT MATCHED THEN
                    INSERT (UserId, RealmId, EntityType, LastUpdatedAfter, Status, CreatedAt, UpdatedAt)
                    VALUES (source.UserId, source.RealmId, source.EntityType, source.LastUpdatedAfter, 'Completed', @UpdatedAt, source.UpdatedAt);";

            using (var connection = CreateOpenConnection())
            {
                await connection.ExecuteAsync(
                    sql,
                    new
                    {
                        UserId = userId,
                        RealmId = realmId,
                        EntityType = entityType,
                        LastUpdatedAfter = lastUpdatedAfter,
                        UpdatedAt = DateTime.UtcNow
                    });
            }
        }

        public async Task UpdateStatusAsync(int userId, string realmId, string entityType, string status)
        {
            var sql = @"
                MERGE QBO_Sync_State AS target
                USING (VALUES (@UserId, @RealmId, @EntityType, @Status, @LastRunAt, @UpdatedAt)) 
                    AS source (UserId, RealmId, EntityType, Status, LastRunAt, UpdatedAt)
                ON target.UserId = source.UserId 
                   AND target.RealmId = source.RealmId 
                   AND target.EntityType = source.EntityType
                WHEN MATCHED THEN
                    UPDATE SET
                        Status = source.Status,
                        LastRunAt = source.LastRunAt,
                        UpdatedAt = source.UpdatedAt
                WHEN NOT MATCHED THEN
                    INSERT (UserId, RealmId, EntityType, Status, LastRunAt, CreatedAt, UpdatedAt)
                    VALUES (source.UserId, source.RealmId, source.EntityType, source.Status, source.LastRunAt, @UpdatedAt, source.UpdatedAt);";

            using (var connection = CreateOpenConnection())
            {
                await connection.ExecuteAsync(
                    sql,
                    new
                    {
                        UserId = userId,
                        RealmId = realmId,
                        EntityType = entityType,
                        Status = status,
                        LastRunAt = status == "Running" ? DateTime.UtcNow : (DateTime?)null,
                        UpdatedAt = DateTime.UtcNow
                    });
            }
        }
    }
}
