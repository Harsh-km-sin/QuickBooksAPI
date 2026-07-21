namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlAccountStats
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AvgAmount { get; set; }
    public double StdDev { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int OutlierCount { get; set; }
}
