using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.Features.ChartOfAccounts.Mapping;

internal static class ChartOfAccountsMutationMapper
{
    public static ChartOfAccountsUpsertDto MapToUpsert(AccountDto a, int userId, string realmId) =>
        new()
        {
            QboId = a.Id ?? string.Empty,
            Name = a.Name ?? string.Empty,
            SubAccount = a.SubAccount,
            FullyQualifiedName = a.FullyQualifiedName ?? string.Empty,
            Active = a.Active,
            Classification = a.Classification ?? string.Empty,
            AccountType = a.AccountType ?? string.Empty,
            AccountSubType = a.AccountSubType ?? string.Empty,
            CurrentBalance = a.CurrentBalance,
            CurrentBalanceWithSubAccounts = a.CurrentBalanceWithSubAccounts,
            CurrencyRefValue = a.CurrencyRef?.Value ?? string.Empty,
            CurrencyRefName = a.CurrencyRef?.Name ?? string.Empty,
            Domain = a.Domain ?? string.Empty,
            Sparse = a.Sparse,
            SyncToken = a.SyncToken ?? string.Empty,
            CreateTime = a.MetaData?.CreateTime ?? DateTime.MinValue,
            LastUpdatedTime = a.MetaData?.LastUpdatedTime ?? DateTime.MinValue,
            UserId = userId,
            RealmId = realmId
        };
}
