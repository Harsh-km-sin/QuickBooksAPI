using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class DimEntityRepository : IDimEntityRepository
    {
        private readonly string _connectionString;

        public DimEntityRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<IReadOnlyList<DimEntity>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode
FROM dbo.dim_entity WHERE UserId = @UserId ORDER BY CASE WHEN ParentEntityId IS NULL THEN 0 ELSE 1 END, ParentEntityId, Id;";
            using var connection = new SqlConnection(_connectionString);
            var list = await connection.QueryAsync<DimEntity>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
            return list?.ToList() ?? new List<DimEntity>();
        }

        public async Task<IReadOnlyList<DimEntity>> GetChildrenAsync(int parentEntityId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode
FROM dbo.dim_entity WHERE ParentEntityId = @ParentEntityId ORDER BY Id;";
            using var connection = new SqlConnection(_connectionString);
            var list = await connection.QueryAsync<DimEntity>(new CommandDefinition(sql, new { ParentEntityId = parentEntityId }, cancellationToken: cancellationToken));
            return list?.ToList() ?? new List<DimEntity>();
        }

        public async Task<DimEntity?> GetByUserAndRealmAsync(int userId, string realmId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT Id, UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode
FROM dbo.dim_entity WHERE UserId = @UserId AND RealmId = @RealmId;";
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<DimEntity>(new CommandDefinition(sql, new { UserId = userId, RealmId = realmId }, cancellationToken: cancellationToken));
        }

        public async Task<IReadOnlyList<DimEntity>> GetParentEntitiesAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT DISTINCT p.Id, p.UserId, p.RealmId, p.ParentEntityId, p.Name, p.Currency, p.IsConsolidatedNode
FROM dbo.dim_entity p
INNER JOIN dbo.dim_entity c ON c.ParentEntityId = p.Id
WHERE p.UserId = @UserId
ORDER BY p.Id;";
            using var connection = new SqlConnection(_connectionString);
            var list = await connection.QueryAsync<DimEntity>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
            return list?.ToList() ?? new List<DimEntity>();
        }

        public async Task<int> UpsertAsync(DimEntity entity, CancellationToken cancellationToken = default)
        {
            const string sql = @"
MERGE dbo.dim_entity AS t
USING (SELECT @UserId AS UserId, @RealmId AS RealmId) AS s ON t.UserId = s.UserId AND t.RealmId = s.RealmId
WHEN MATCHED THEN
  UPDATE SET ParentEntityId = @ParentEntityId, Name = @Name, Currency = @Currency, IsConsolidatedNode = @IsConsolidatedNode
WHEN NOT MATCHED THEN
  INSERT (UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode)
  VALUES (@UserId, @RealmId, @ParentEntityId, @Name, @Currency, @IsConsolidatedNode)
OUTPUT INSERTED.Id;";
            using var connection = new SqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
            {
                entity.UserId,
                entity.RealmId,
                entity.ParentEntityId,
                entity.Name,
                entity.Currency,
                entity.IsConsolidatedNode
            }, cancellationToken: cancellationToken));
            return id;
        }
    }
}
