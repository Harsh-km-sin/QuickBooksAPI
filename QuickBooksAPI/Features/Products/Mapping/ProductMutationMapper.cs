using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.Features.Products.Mapping;

internal static class ProductMutationMapper
{
    public static ProductUpsertDto MapToUpsert(QuickBooksItemDto dto, int userId, string realmId) =>
        new()
        {
            QboId = dto.QBOId,
            Name = dto.Name,
            Description = dto.Description,
            Active = dto.Active,
            FullyQualifiedName = dto.FullyQualifiedName,
            Taxable = dto.Taxable,
            UnitPrice = dto.UnitPrice,
            Type = dto.Type,
            QtyOnHand = dto.QtyOnHand ?? 0,
            IncomeAccountRefValue = dto.IncomeAccountRef?.Value,
            IncomeAccountRefName = dto.IncomeAccountRef?.Name,
            ExpenseAccountRefValue = dto.ExpenseAccountRef?.Value,
            ExpenseAccountRefName = dto.ExpenseAccountRef?.Name,
            AssetAccountRefValue = dto.AssetAccountRef?.Value,
            AssetAccountRefName = dto.AssetAccountRef?.Name,
            PurchaseCost = dto.PurchaseCost,
            TrackQtyOnHand = dto.TrackQtyOnHand,
            InvStartDate = dto.InvStartDate,
            Domain = dto.Domain,
            Sparse = dto.Sparse,
            SyncToken = dto.SyncToken,
            CreateTime = dto.MetaData?.CreateTime ?? DateTime.Now,
            LastUpdatedTime = dto.MetaData?.LastUpdatedTime ?? DateTime.Now,
            UserId = userId,
            RealmId = realmId
        };
}
