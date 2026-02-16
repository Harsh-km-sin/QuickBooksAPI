using Dapper;
using Microsoft.Data.SqlClient;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;
using System.Linq;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public class ChartOfAccountsRepository : IChartOfAccountsRepository
    {
        private readonly string _connectionString;

        public ChartOfAccountsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task<IEnumerable<ChartOfAccounts>> GetAllByUserAndRealmAsync(int userId, string realmId)
        {
            using var connection = CreateOpenConnection();
            const string sql = @"
                SELECT Id, QBOId, Name, SubAccount, FullyQualifiedName, Active, Classification,
                    AccountType, AccountSubType, CurrentBalance, CurrentBalanceWithSubAccounts,
                    CurrencyRefValue, CurrencyRefName, Domain, Sparse, SyncToken,
                    CreateTime, LastUpdatedTime, UserId, RealmId
                FROM dbo.ChartOfAccounts
                WHERE UserId = @UserId AND RealmId = @RealmId
                ORDER BY FullyQualifiedName";
            return await connection.QueryAsync<ChartOfAccounts>(sql, new { UserId = userId, RealmId = realmId });
        }

        public async Task<PagedResult<ChartOfAccounts>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search)
        {
            using var connection = CreateOpenConnection();
            var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var skip = (page - 1) * pageSize;

            var countSql = @"
                SELECT COUNT(*) FROM dbo.ChartOfAccounts 
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@Search IS NULL OR Name LIKE @Search OR FullyQualifiedName LIKE @Search OR AccountType LIKE @Search)";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId, RealmId = realmId, Search = searchPattern });

            var itemsSql = @"
                SELECT Id, QBOId, Name, SubAccount, FullyQualifiedName, Active, Classification,
                    AccountType, AccountSubType, CurrentBalance, CurrentBalanceWithSubAccounts,
                    CurrencyRefValue, CurrencyRefName, Domain, Sparse, SyncToken,
                    CreateTime, LastUpdatedTime, UserId, RealmId
                FROM dbo.ChartOfAccounts
                WHERE UserId = @UserId AND RealmId = @RealmId
                AND (@Search IS NULL OR Name LIKE @Search OR FullyQualifiedName LIKE @Search OR AccountType LIKE @Search)
                ORDER BY FullyQualifiedName
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<ChartOfAccounts>(itemsSql, new { UserId = userId, RealmId = realmId, Search = searchPattern, Skip = skip, PageSize = pageSize });

            return new PagedResult<ChartOfAccounts> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<int> UpsertChartOfAccountsAsync(IEnumerable<ChartOfAccounts> accounts)
        {
            if (accounts == null || !accounts.Any())
                return 0;

            using var connection = CreateOpenConnection();

            var accountsTable = BuildChartOfAccountsTable(accounts);
            var parameters = new DynamicParameters();
            parameters.Add("@Accounts", accountsTable.AsTableValuedParameter("dbo.ChartOfAccountsUpsertType"));

            return await connection.ExecuteAsync("dbo.UpsertChartOfAccounts", parameters, commandType: CommandType.StoredProcedure);
        }

        private static DataTable BuildChartOfAccountsTable(IEnumerable<ChartOfAccounts> accounts)
        {
            var table = new DataTable();
            table.Columns.Add("QBOId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("SubAccount", typeof(bool));
            table.Columns.Add("FullyQualifiedName", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("Classification", typeof(string));
            table.Columns.Add("AccountType", typeof(string));
            table.Columns.Add("AccountSubType", typeof(string));
            table.Columns.Add("CurrentBalance", typeof(decimal));
            table.Columns.Add("CurrentBalanceWithSubAccounts", typeof(decimal));
            table.Columns.Add("CurrencyRefValue", typeof(string));
            table.Columns.Add("CurrencyRefName", typeof(string));
            table.Columns.Add("Domain", typeof(string));
            table.Columns.Add("Sparse", typeof(bool));
            table.Columns.Add("SyncToken", typeof(string));
            table.Columns.Add("CreateTime", typeof(DateTime));
            table.Columns.Add("LastUpdatedTime", typeof(DateTime));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("RealmId", typeof(string));

            foreach (var a in accounts)
            {
                table.Rows.Add(
                    a.QBOId,
                    a.Name,
                    a.SubAccount,
                    a.FullyQualifiedName,
                    a.Active,
                    string.IsNullOrEmpty(a.Classification) ? DBNull.Value : a.Classification,
                    string.IsNullOrEmpty(a.AccountType) ? DBNull.Value : a.AccountType,
                    string.IsNullOrEmpty(a.AccountSubType) ? DBNull.Value : a.AccountSubType,
                    a.CurrentBalance,
                    a.CurrentBalanceWithSubAccounts,
                    string.IsNullOrEmpty(a.CurrencyRefValue) ? DBNull.Value : a.CurrencyRefValue,
                    string.IsNullOrEmpty(a.CurrencyRefName) ? DBNull.Value : a.CurrencyRefName,
                    string.IsNullOrEmpty(a.Domain) ? DBNull.Value : a.Domain,
                    a.Sparse,
                    a.SyncToken,
                    a.CreateTime,
                    a.LastUpdatedTime,
                    a.UserId,
                    a.RealmId);
            }
            return table;
        }
    }
}
