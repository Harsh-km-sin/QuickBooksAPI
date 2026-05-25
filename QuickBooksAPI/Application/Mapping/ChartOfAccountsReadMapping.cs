using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using CoaRow = QuickBooksAPI.DataAccessLayer.Models.ChartOfAccounts;

namespace QuickBooksAPI.Application.Mapping;

internal static class ChartOfAccountsReadMapping
{
    public static ChartOfAccountsItemDto ToDto(CoaRow a) => new()
    {
        Id = a.Id,
        QboId = a.QBOId,
        Name = a.Name,
        SubAccount = a.SubAccount,
        FullyQualifiedName = a.FullyQualifiedName,
        Active = a.Active,
        Classification = a.Classification,
        AccountType = a.AccountType,
        AccountSubType = a.AccountSubType,
        CurrentBalance = a.CurrentBalance,
        CurrentBalanceWithSubAccounts = a.CurrentBalanceWithSubAccounts,
        CurrencyRefValue = a.CurrencyRefValue,
        CurrencyRefName = a.CurrencyRefName,
        Domain = a.Domain,
        Sparse = a.Sparse,
        SyncToken = a.SyncToken,
        CreateTime = a.CreateTime,
        LastUpdatedTime = a.LastUpdatedTime,
        UserId = a.UserId,
        RealmId = a.RealmId
    };

    public static PagedResult<ChartOfAccountsItemDto> ToDtoPaged(PagedResult<CoaRow> paged)
    {
        var items = paged.Items.Select(ToDto).ToList();
        return new PagedResult<ChartOfAccountsItemDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
