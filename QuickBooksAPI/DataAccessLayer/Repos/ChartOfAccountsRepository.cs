using Dapper;
using Microsoft.Data.SqlClient;
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

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<int> UpsertChartOfAccountsAsync(IEnumerable<ChartOfAccounts> accounts)
        {
            using var connection = CreateConnection();

            var sql = @"
                        MERGE ChartOfAccounts AS target
                        USING (VALUES
                            {0}
                        ) AS source
                        (QBOId, Name, SubAccount, FullyQualifiedName, Active, Classification, AccountType, AccountSubType,
                         CurrentBalance, CurrentBalanceWithSubAccounts, CurrencyRefValue, CurrencyRefName, Domain, Sparse, SyncToken,
                         CreateTime, LastUpdatedTime, UserId, RealmId)
                        ON target.QBOId = source.QBOId AND target.UserId = source.UserId AND target.RealmId = source.RealmId
                        WHEN MATCHED THEN
                            UPDATE SET
                                Name = source.Name,
                                SubAccount = source.SubAccount,
                                FullyQualifiedName = source.FullyQualifiedName,
                                Active = source.Active,
                                Classification = source.Classification,
                                AccountType = source.AccountType,
                                AccountSubType = source.AccountSubType,
                                CurrentBalance = source.CurrentBalance,
                                CurrentBalanceWithSubAccounts = source.CurrentBalanceWithSubAccounts,
                                CurrencyRefValue = source.CurrencyRefValue,
                                CurrencyRefName = source.CurrencyRefName,
                                Domain = source.Domain,
                                Sparse = source.Sparse,
                                SyncToken = source.SyncToken,
                                CreateTime = source.CreateTime,
                                LastUpdatedTime = source.LastUpdatedTime
                        WHEN NOT MATCHED THEN
                            INSERT (QBOId, Name, SubAccount, FullyQualifiedName, Active, Classification, AccountType, AccountSubType,
                                    CurrentBalance, CurrentBalanceWithSubAccounts, CurrencyRefValue, CurrencyRefName, Domain, Sparse, SyncToken,
                                    CreateTime, LastUpdatedTime, UserId, RealmId)
                            VALUES (source.QBOId, source.Name, source.SubAccount, source.FullyQualifiedName, source.Active, source.Classification,
                                    source.AccountType, source.AccountSubType, source.CurrentBalance, source.CurrentBalanceWithSubAccounts,
                                    source.CurrencyRefValue, source.CurrencyRefName, source.Domain, source.Sparse, source.SyncToken,
                                    source.CreateTime, source.LastUpdatedTime, source.UserId, source.RealmId);";

            var valuesList = accounts.Select((a, i) =>
                $"(@QBOId{i}, @Name{i}, @SubAccount{i}, @FullyQualifiedName{i}, @Active{i}, @Classification{i}, @AccountType{i}, @AccountSubType{i}, " +
                $"@CurrentBalance{i}, @CurrentBalanceWithSubAccounts{i}, @CurrencyRefValue{i}, @CurrencyRefName{i}, @Domain{i}, @Sparse{i}, @SyncToken{i}, " +
                $"@CreateTime{i}, @LastUpdatedTime{i}, @UserId{i}, @RealmId{i})");

            sql = string.Format(sql, string.Join(", ", valuesList));

            var parameters = new DynamicParameters();
            int idx = 0;
            foreach (var a in accounts)
            {
                parameters.Add($"@QBOId{idx}", a.QBOId);
                parameters.Add($"@Name{idx}", a.Name);
                parameters.Add($"@SubAccount{idx}", a.SubAccount);
                parameters.Add($"@FullyQualifiedName{idx}", a.FullyQualifiedName);
                parameters.Add($"@Active{idx}", a.Active);
                parameters.Add($"@Classification{idx}", a.Classification);
                parameters.Add($"@AccountType{idx}", a.AccountType);
                parameters.Add($"@AccountSubType{idx}", a.AccountSubType);
                parameters.Add($"@CurrentBalance{idx}", a.CurrentBalance);
                parameters.Add($"@CurrentBalanceWithSubAccounts{idx}", a.CurrentBalanceWithSubAccounts);
                parameters.Add($"@CurrencyRefValue{idx}", a.CurrencyRefValue);
                parameters.Add($"@CurrencyRefName{idx}", a.CurrencyRefName);
                parameters.Add($"@Domain{idx}", a.Domain);
                parameters.Add($"@Sparse{idx}", a.Sparse);
                parameters.Add($"@SyncToken{idx}", a.SyncToken);
                parameters.Add($"@CreateTime{idx}", a.CreateTime);
                parameters.Add($"@LastUpdatedTime{idx}", a.LastUpdatedTime);
                parameters.Add($"@UserId{idx}", a.UserId);
                parameters.Add($"@RealmId{idx}", a.RealmId);
                idx++;
            }

            return await connection.ExecuteAsync(sql, parameters);
        }
    }
}
