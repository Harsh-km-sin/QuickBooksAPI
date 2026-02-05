using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<int> UpsertProductsAsync(IEnumerable<Products> products)
        {
            using var connection = CreateConnection();

            var sql = @"
                MERGE Products AS target
                USING (VALUES
                    {0}
                ) AS source
                (QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
                    IncomeAccountRefValue, IncomeAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
                    Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId)
                ON target.QBOId = source.QBOId AND target.UserId = source.UserId AND target.RealmId = source.RealmId
                WHEN MATCHED THEN
                    UPDATE SET
                        Name = source.Name,
                        Description = source.Description,
                        Active = source.Active,
                        FullyQualifiedName = source.FullyQualifiedName,
                        Taxable = source.Taxable,
                        UnitPrice = source.UnitPrice,
                        Type = source.Type,
                        IncomeAccountRefValue = source.IncomeAccountRefValue,
                        IncomeAccountRefName = source.IncomeAccountRefName,
                        PurchaseCost = source.PurchaseCost,
                        TrackQtyOnHand = source.TrackQtyOnHand,
                        QtyOnHand = source.QtyOnHand,
                        Domain = source.Domain,
                        Sparse = source.Sparse,
                        SyncToken = source.SyncToken,
                        CreateTime = source.CreateTime,
                        LastUpdatedTime = source.LastUpdatedTime
                WHEN NOT MATCHED THEN
                    INSERT (
                        QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
                        IncomeAccountRefValue, IncomeAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
                        Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId
                    )
                    VALUES (
                        source.QBOId, source.Name, source.Description, source.Active, source.FullyQualifiedName, source.Taxable,
                        source.UnitPrice, source.Type, source.IncomeAccountRefValue, source.IncomeAccountRefName,
                        source.PurchaseCost, source.TrackQtyOnHand, source.QtyOnHand, source.Domain, source.Sparse, source.SyncToken,
                        source.CreateTime, source.LastUpdatedTime, source.UserId, source.RealmId
                    );";

            var valuesList = products.Select((p, i) =>
                $"(@QBOId{i}, @Name{i}, @Description{i}, @Active{i}, @FullyQualifiedName{i}, @Taxable{i}, @UnitPrice{i}, @Type{i}, " +
                $"@IncomeAccountRefValue{i}, @IncomeAccountRefName{i}, @PurchaseCost{i}, @TrackQtyOnHand{i}, @QtyOnHand{i}, " +
                $"@Domain{i}, @Sparse{i}, @SyncToken{i}, @CreateTime{i}, @LastUpdatedTime{i}, @UserId{i}, @RealmId{i})");



            sql = string.Format(sql, string.Join(", ", valuesList));

            var parameters = new DynamicParameters();
            int idx = 0;
            foreach (var p in products)
            {
                parameters.Add($"@QBOId{idx}", p.QBOId);
                parameters.Add($"@Name{idx}", p.Name);
                parameters.Add($"@Description{idx}", p.Description);
                parameters.Add($"@Active{idx}", p.Active);
                parameters.Add($"@FullyQualifiedName{idx}", p.FullyQualifiedName);
                parameters.Add($"@Taxable{idx}", p.Taxable);
                parameters.Add($"@UnitPrice{idx}", p.UnitPrice);
                parameters.Add($"@Type{idx}", p.Type);
                parameters.Add($"@IncomeAccountRefValue{idx}", p.IncomeAccountRefValue);
                parameters.Add($"@IncomeAccountRefName{idx}", p.IncomeAccountRefName);
                parameters.Add($"@PurchaseCost{idx}", p.PurchaseCost);
                parameters.Add($"@TrackQtyOnHand{idx}", p.TrackQtyOnHand);
                parameters.Add($"@QtyOnHand{idx}", p.QtyOnHand);
                parameters.Add($"@Domain{idx}", p.Domain);
                parameters.Add($"@Sparse{idx}", p.Sparse);
                parameters.Add($"@SyncToken{idx}", p.SyncToken);
                parameters.Add($"@CreateTime{idx}", p.CreateTime);
                parameters.Add($"@LastUpdatedTime{idx}", p.LastUpdatedTime);
                parameters.Add($"@UserId{idx}", p.UserId);
                parameters.Add($"@RealmId{idx}", p.RealmId);
                idx++;
            }

            return await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT MAX(LastUpdatedTime) 
                FROM Products 
                WHERE UserId = @UserId AND RealmId = @RealmId";

            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { UserId = userId, RealmId = realmId });
        }
    }
}
