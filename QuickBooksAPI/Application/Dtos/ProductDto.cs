namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Product row returned by list APIs; maps from persistence (<see cref="DataAccessLayer.Models.Products"/>).
/// </summary>
public sealed class ProductDto
{
    public int Id { get; set; }
    public string QboId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
    public string FullyQualifiedName { get; set; } = string.Empty;
    public bool Taxable { get; set; }
    public decimal UnitPrice { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal? QtyOnHand { get; set; }
    public string? IncomeAccountRefValue { get; set; }
    public string? IncomeAccountRefName { get; set; }
    public string? ExpenseAccountRefValue { get; set; }
    public string? ExpenseAccountRefName { get; set; }
    public string? AssetAccountRefValue { get; set; }
    public string? AssetAccountRefName { get; set; }
    public decimal PurchaseCost { get; set; }
    public bool TrackQtyOnHand { get; set; }
    public string? InvStartDate { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool Sparse { get; set; }
    public string SyncToken { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public int UserId { get; set; }
    public string RealmId { get; set; } = string.Empty;
}
