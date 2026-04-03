namespace QuickBooksAPI.DataAccessLayer.Warehouse;

/// <summary>
/// Central place for warehouse analytics assumptions (COGS proxies, caps, etc.).
/// </summary>
public static class WarehouseAnalyticsPolicy
{
    /// <summary>
    /// Simple revenue-based COGS proxy used when rebuilding <c>FactCustomerProfitability</c> (not line-level COGS from QBO).
    /// </summary>
    public const decimal DefaultCustomerCogsRatio = 0.4m;
}
