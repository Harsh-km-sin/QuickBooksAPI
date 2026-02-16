using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<int> UpsertCustomersAsync(IEnumerable<Customer> customers, int userId, string realmId)
        {
            if (customers == null || !customers.Any()) return 0;

            using var connection = CreateConnection();

            var table = new DataTable();
            table.Columns.Add("QboId", typeof(string));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("GivenName", typeof(string));
            table.Columns.Add("FamilyName", typeof(string));
            table.Columns.Add("DisplayName", typeof(string));
            table.Columns.Add("CompanyName", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("Balance", typeof(decimal));
            table.Columns.Add("PrimaryEmailAddr", typeof(string));
            table.Columns.Add("PrimaryPhone", typeof(string));
            table.Columns.Add("BillAddrLine1", typeof(string));
            table.Columns.Add("BillAddrCity", typeof(string));
            table.Columns.Add("BillAddrPostalCode", typeof(string));
            table.Columns.Add("BillAddrCountrySubDivisionCode", typeof(string));
            table.Columns.Add("CreateTime", typeof(DateTime));
            table.Columns.Add("LastUpdatedTime", typeof(DateTime));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var c in customers)
            {
                table.Rows.Add(
                    c.QboId,
                    c.SyncToken,
                    c.GivenName,
                    c.FamilyName,
                    c.DisplayName,
                    c.CompanyName,
                    c.Active,
                    c.Balance,
                    c.PrimaryEmailAddr,
                    c.PrimaryPhone,
                    c.BillAddrLine1,
                    c.BillAddrCity,
                    c.BillAddrPostalCode,
                    c.BillAddrCountrySubDivisionCode,
                    c.CreateTime,
                    c.LastUpdatedTime,
                    userId,
                    realmId
                );
            }

            var parameters = new DynamicParameters();
            parameters.Add(
                "@Customers",
                table.AsTableValuedParameter("dbo.CustomerUpsertType")
            );

            return await connection.ExecuteAsync(
                "dbo.UpsertCustomer",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT MAX(LastUpdatedTime) 
                FROM Customer 
                WHERE UserId = @UserId AND RealmId = @RealmId";

            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { UserId = userId, RealmId = realmId });
        }

        public async Task<IEnumerable<Customer>> GetAllByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = CreateConnection();
            const string sql = @"
                SELECT Id, QboId, SyncToken, GivenName, FamilyName, DisplayName, CompanyName,
                    Active, Balance, PrimaryEmailAddr, PrimaryPhone, BillAddrLine1, BillAddrCity, BillAddrPostalCode,
                    BillAddrCountrySubDivisionCode, CreateTime, LastUpdatedTime, UserId, RealmId
                FROM Customer WHERE UserId = @UserId AND RealmId = @RealmId ORDER BY DisplayName";
            return await connection.QueryAsync<Customer>(sql, new { UserId = userId, RealmId = realmId });
        }

        public async Task<PagedResult<Customer>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search)
        {
            using var connection = CreateConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM Customer 
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@Search IS NULL OR
                    DisplayName LIKE @Search OR
                    GivenName LIKE @Search OR
                    FamilyName LIKE @Search OR
                    CompanyName LIKE @Search OR
                    PrimaryEmailAddr LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId, RealmId = realmId, Search = searchPattern });

            var itemsSql = @"
                SELECT Id, QboId, SyncToken, GivenName, FamilyName, DisplayName, CompanyName,
                    Active, Balance, PrimaryEmailAddr, PrimaryPhone, BillAddrLine1, BillAddrCity, BillAddrPostalCode,
                    BillAddrCountrySubDivisionCode, CreateTime, LastUpdatedTime, UserId, RealmId
                FROM Customer 
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@Search IS NULL OR
                    DisplayName LIKE @Search OR
                    GivenName LIKE @Search OR
                    FamilyName LIKE @Search OR
                    CompanyName LIKE @Search OR
                    PrimaryEmailAddr LIKE @Search)
                ORDER BY DisplayName
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<Customer>(itemsSql, new { UserId = userId, RealmId = realmId, Search = searchPattern, Skip = skip, PageSize = pageSize });

            return new PagedResult<Customer>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
