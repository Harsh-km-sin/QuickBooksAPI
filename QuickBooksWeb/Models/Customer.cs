namespace QuickBooksWeb.Models;

public class Customer
{
    public int Id { get; set; }
    public string QboId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? PrimaryEmailAddr { get; set; }
    public string? PrimaryPhone { get; set; }
    public bool Active { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
}
