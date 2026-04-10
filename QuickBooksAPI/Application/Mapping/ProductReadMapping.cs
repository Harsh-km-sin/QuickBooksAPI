using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Mapping;

internal static class ProductReadMapping
{
    public static ProductDto ToDto(Products p) => new()
    {
        Id = p.Id,
        QboId = p.QBOId,
        Name = p.Name,
        Description = p.Description,
        Active = p.Active,
        FullyQualifiedName = p.FullyQualifiedName,
        Taxable = p.Taxable,
        UnitPrice = p.UnitPrice,
        Type = p.Type,
        QtyOnHand = p.QtyOnHand,
        IncomeAccountRefValue = p.IncomeAccountRefValue,
        IncomeAccountRefName = p.IncomeAccountRefName,
        ExpenseAccountRefValue = p.ExpenseAccountRefValue,
        ExpenseAccountRefName = p.ExpenseAccountRefName,
        AssetAccountRefValue = p.AssetAccountRefValue,
        AssetAccountRefName = p.AssetAccountRefName,
        PurchaseCost = p.PurchaseCost,
        TrackQtyOnHand = p.TrackQtyOnHand,
        InvStartDate = p.InvStartDate,
        Domain = p.Domain,
        Sparse = p.Sparse,
        SyncToken = p.SyncToken,
        CreateTime = p.CreateTime,
        LastUpdatedTime = p.LastUpdatedTime,
        UserId = p.UserId,
        RealmId = p.RealmId
    };

    public static PagedResult<ProductDto> ToDtoPaged(PagedResult<Products> paged)
    {
        var items = paged.Items.Select(ToDto).ToList();
        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
