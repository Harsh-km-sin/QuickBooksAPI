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

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task<int> UpsertProductsAsync(IEnumerable<Products> products)
        {
            if (products == null || !products.Any())
                return 0;

            using var connection = CreateOpenConnection();

            var productsTable = BuildProductTable(products);
            var parameters = new DynamicParameters();
            parameters.Add("@Products", productsTable.AsTableValuedParameter("dbo.ProductUpsertType"));

            return await connection.ExecuteAsync("dbo.UpsertProduct", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId)
        {
            using var connection = CreateOpenConnection();
            string sql = @"
                SELECT MAX(LastUpdatedTime) 
                FROM Products 
                WHERE UserId = @UserId AND RealmId = @RealmId";

            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { UserId = userId, RealmId = realmId });
        }

        private static DataTable BuildProductTable(IEnumerable<Products> products)
        {
            var table = new DataTable();
            table.Columns.Add("QBOId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("FullyQualifiedName", typeof(string));
            table.Columns.Add("Taxable", typeof(bool));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("IncomeAccountRefValue", typeof(string));
            table.Columns.Add("IncomeAccountRefName", typeof(string));
            table.Columns.Add("PurchaseCost", typeof(decimal));
            table.Columns.Add("TrackQtyOnHand", typeof(bool));
            table.Columns.Add("QtyOnHand", typeof(decimal));
            table.Columns.Add("Domain", typeof(string));
            table.Columns.Add("Sparse", typeof(bool));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("CreateTime", typeof(DateTime));
            table.Columns.Add("LastUpdatedTime", typeof(DateTime));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var p in products)
            {
                table.Rows.Add(
                    p.QBOId,
                    p.Name,
                    string.IsNullOrEmpty(p.Description) ? DBNull.Value : p.Description,
                    p.Active,
                    p.FullyQualifiedName,
                    p.Taxable,
                    p.UnitPrice,
                    p.Type,
                    string.IsNullOrEmpty(p.IncomeAccountRefValue) ? DBNull.Value : p.IncomeAccountRefValue,
                    string.IsNullOrEmpty(p.IncomeAccountRefName) ? DBNull.Value : p.IncomeAccountRefName,
                    p.PurchaseCost,
                    p.TrackQtyOnHand,
                    p.QtyOnHand ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(p.Domain) ? DBNull.Value : p.Domain,
                    p.Sparse,
                    p.SyncToken,
                    p.CreateTime,
                    p.LastUpdatedTime,
                    p.UserId,
                    p.RealmId);
            }
            return table;
        }
    }
}
