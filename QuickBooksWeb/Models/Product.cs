namespace QuickBooksWeb.Models;

public class Product
{
    public int Id { get; set; }
    public string QBOId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool Active { get; set; }
    public string FullyQualifiedName { get; set; } = null!;
    public bool Taxable { get; set; }
    public decimal UnitPrice { get; set; }
    public string Type { get; set; } = null!;
    public decimal? QtyOnHand { get; set; }
    public string? IncomeAccountRefValue { get; set; }
    public string? IncomeAccountRefName { get; set; }
    public decimal PurchaseCost { get; set; }
    public bool TrackQtyOnHand { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
}
