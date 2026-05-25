using Dapper;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ProductRepository : IProductRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ProductRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = _connectionFactory.CreateConnection();
            conn.Open();
            return conn;
        }

        public async Task<int> UpsertProductsAsync(IEnumerable<ProductUpsertDto> products)
        {
            if (products == null || !products.Any())
                return 0;

            using var connection = CreateOpenConnection();

            var rows = products.Select(ToPersistenceRow).ToList();
            var productsTable = BuildProductTable(rows);
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

        public async Task<IEnumerable<ProductDto>> GetAllByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"
                SELECT Id, QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
                    IncomeAccountRefValue, IncomeAccountRefName, ExpenseAccountRefValue, ExpenseAccountRefName,
                    AssetAccountRefValue, AssetAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
                    InvStartDate, Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId
                FROM Products WHERE UserId = @UserId AND RealmId = @RealmId AND Active = 1 ORDER BY Name";
            var rows = await connection.QueryAsync<Products>(sql, new { UserId = userId, RealmId = realmId });
            return rows.Select(ProductReadMapping.ToDto);
        }

        public async Task<PagedResult<ProductDto>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search, bool? activeFilter = true)
        {
            using var connection = CreateOpenConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM Products 
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@ActiveFilter IS NULL OR Active = @ActiveFilter)
                AND (@Search IS NULL OR Name LIKE @Search OR Description LIKE @Search OR FullyQualifiedName LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId, RealmId = realmId, Search = searchPattern, ActiveFilter = activeFilter });

            var itemsSql = @"
                SELECT Id, QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
                    IncomeAccountRefValue, IncomeAccountRefName, ExpenseAccountRefValue, ExpenseAccountRefName,
                    AssetAccountRefValue, AssetAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
                    InvStartDate, Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId
                FROM Products 
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@ActiveFilter IS NULL OR Active = @ActiveFilter)
                AND (@Search IS NULL OR Name LIKE @Search OR Description LIKE @Search OR FullyQualifiedName LIKE @Search)
                ORDER BY Name
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<Products>(itemsSql, new { UserId = userId, RealmId = realmId, Search = searchPattern, ActiveFilter = activeFilter, Skip = skip, PageSize = pageSize });
            var dtoItems = items.Select(ProductReadMapping.ToDto).ToList();

            return new PagedResult<ProductDto> { Items = dtoItems, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        private static Products ToPersistenceRow(ProductUpsertDto d) =>
            new()
            {
                QBOId = d.QboId,
                Name = d.Name,
                Description = d.Description,
                Active = d.Active,
                FullyQualifiedName = d.FullyQualifiedName,
                Taxable = d.Taxable,
                UnitPrice = d.UnitPrice,
                Type = d.Type,
                QtyOnHand = d.QtyOnHand ?? 0,
                IncomeAccountRefValue = d.IncomeAccountRefValue,
                IncomeAccountRefName = d.IncomeAccountRefName,
                ExpenseAccountRefValue = d.ExpenseAccountRefValue,
                ExpenseAccountRefName = d.ExpenseAccountRefName,
                AssetAccountRefValue = d.AssetAccountRefValue,
                AssetAccountRefName = d.AssetAccountRefName,
                PurchaseCost = d.PurchaseCost,
                TrackQtyOnHand = d.TrackQtyOnHand,
                InvStartDate = d.InvStartDate,
                Domain = d.Domain,
                Sparse = d.Sparse,
                SyncToken = d.SyncToken,
                CreateTime = d.CreateTime,
                LastUpdatedTime = d.LastUpdatedTime,
                UserId = d.UserId,
                RealmId = d.RealmId
            };

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
            table.Columns.Add("ExpenseAccountRefValue", typeof(string));
            table.Columns.Add("ExpenseAccountRefName", typeof(string));
            table.Columns.Add("AssetAccountRefValue", typeof(string));
            table.Columns.Add("AssetAccountRefName", typeof(string));
            table.Columns.Add("PurchaseCost", typeof(decimal));
            table.Columns.Add("TrackQtyOnHand", typeof(bool));
            table.Columns.Add("QtyOnHand", typeof(decimal));
            table.Columns.Add("InvStartDate", typeof(string));
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
                    string.IsNullOrEmpty(p.ExpenseAccountRefValue) ? DBNull.Value : p.ExpenseAccountRefValue,
                    string.IsNullOrEmpty(p.ExpenseAccountRefName) ? DBNull.Value : p.ExpenseAccountRefName,
                    string.IsNullOrEmpty(p.AssetAccountRefValue) ? DBNull.Value : p.AssetAccountRefValue,
                    string.IsNullOrEmpty(p.AssetAccountRefName) ? DBNull.Value : p.AssetAccountRefName,
                    p.PurchaseCost,
                    p.TrackQtyOnHand,
                    p.QtyOnHand ?? (object)DBNull.Value,
                    string.IsNullOrEmpty(p.InvStartDate) ? DBNull.Value : p.InvStartDate,
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
