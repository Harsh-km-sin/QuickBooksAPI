using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class VendorRepository : IVendorRepository
    {
        private readonly string _connectionString;
        public VendorRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<int> UpsertVendorsAsync(IEnumerable<Vendor> vendors, int userId, string realmId)
        {
            if (vendors == null || !vendors.Any()) return 0;

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
            table.Columns.Add("CreateTime", typeof(DateTimeOffset));
            table.Columns.Add("LastUpdatedTime", typeof(DateTimeOffset));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var v in vendors)
            {
                table.Rows.Add(
                    v.QboId,
                    v.SyncToken,
                    v.GivenName,
                    v.FamilyName,
                    v.DisplayName,
                    v.CompanyName,
                    v.Active,
                    v.Balance,
                    v.PrimaryEmailAddr,
                    v.PrimaryPhone,
                    v.BillAddrLine1,
                    v.BillAddrCity,
                    v.BillAddrPostalCode,
                    v.BillAddrCountrySubDivisionCode,
                    v.CreateTime,
                    v.LastUpdatedTime,
                    userId,
                    realmId
                );
            }

            var parameters = new DynamicParameters();
            parameters.Add(
                "@Vendors",
                table.AsTableValuedParameter("dbo.VendorUpsertType")
            );

            return await connection.ExecuteAsync(
                "dbo.UpsertVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Vendor>> GetAllByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = CreateConnection();
            const string sql = @"
                SELECT QboId, UserId, RealmId, SyncToken, GivenName, FamilyName,
                    DisplayName, CompanyName, Active, Balance, PrimaryEmailAddr, PrimaryPhone,
                    BillAddrLine1, BillAddrCity, BillAddrPostalCode, BillAddrCountrySubDivisionCode,
                    CreateTime, LastUpdatedTime
                FROM dbo.Vendor
                WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL)
                ORDER BY DisplayName";
            return await connection.QueryAsync<Vendor>(sql, new { UserId = userId, RealmId = realmId });
        }

        public async Task<PagedResult<Vendor>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search)
        {
            using var connection = CreateConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM dbo.Vendor 
                WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL)
                AND (@Search IS NULL OR DisplayName LIKE @Search OR GivenName LIKE @Search OR FamilyName LIKE @Search OR CompanyName LIKE @Search OR PrimaryEmailAddr LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId, RealmId = realmId, Search = searchPattern });

            var itemsSql = @"
                SELECT QboId, UserId, RealmId, SyncToken, GivenName, FamilyName,
                    DisplayName, CompanyName, Active, Balance, PrimaryEmailAddr, PrimaryPhone,
                    BillAddrLine1, BillAddrCity, BillAddrPostalCode, BillAddrCountrySubDivisionCode,
                    CreateTime, LastUpdatedTime
                FROM dbo.Vendor
                WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL)
                AND (@Search IS NULL OR DisplayName LIKE @Search OR GivenName LIKE @Search OR FamilyName LIKE @Search OR CompanyName LIKE @Search OR PrimaryEmailAddr LIKE @Search)
                ORDER BY DisplayName
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<Vendor>(itemsSql, new { UserId = userId, RealmId = realmId, Search = searchPattern, Skip = skip, PageSize = pageSize });

            return new PagedResult<Vendor> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT MAX(LastUpdatedTime) 
                FROM Vendor 
                WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL)";

            var offset = await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(sql, new { UserId = userId, RealmId = realmId });
            return offset?.UtcDateTime;
        }

        public async Task<bool> SoftDeleteAsync(int userId, string realmId, string qboId)
        {
            using var connection = CreateConnection();
            const string sql = @"
                UPDATE Vendor 
                SET DeletedAt = @DeletedAt, DeletedBy = @DeletedBy 
                WHERE UserId = @UserId AND RealmId = @RealmId AND QboId = @QboId AND (DeletedAt IS NULL)";
            var deletedAt = DateTime.UtcNow;
            var deletedBy = userId.ToString();
            var rows = await connection.ExecuteAsync(sql, new { UserId = userId, RealmId = realmId, QboId = qboId, DeletedAt = deletedAt, DeletedBy = deletedBy });
            return rows > 0;
        }
    }
}
