namespace QuickBooksAPI.DataAccessLayer.Models
{
    /// <summary>
    /// Dimension and fact models backing the financial warehouse.
    /// These map directly to SQL tables used for analytics.
    /// </summary>
    public class DimCustomer
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string CustomerQboId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class DimVendor
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string VendorQboId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
    }

    public class DimAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string AccountQboId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
    }

    public class FactRevenue
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int? CustomerDimId { get; set; }
        public int? AccountDimId { get; set; }
        public string InvoiceQboId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class FactExpenses
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int? VendorDimId { get; set; }
        public int? AccountDimId { get; set; }
        public string BillQboId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class FactVendorSpend
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public int VendorDimId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalSpend { get; set; }
        public int BillCount { get; set; }
        public DateTime? LastBillDate { get; set; }
    }

    public class FactCustomerProfitability
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public int CustomerDimId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Revenue { get; set; }
        public decimal CostOfGoods { get; set; }
        public decimal GrossMargin => Revenue - CostOfGoods;
    }

    /// <summary>
    /// Entity (company/subsidiary) for consolidated reporting.
    /// </summary>
    public class DimEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public int? ParentEntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public bool IsConsolidatedNode { get; set; }
    }

    /// <summary>
    /// Consolidated P&L per parent entity per period (after FX and rollup).
    /// </summary>
    public class FactConsolidatedPnl
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
        public decimal? FxRateApplied { get; set; }
        public string? MetadataJson { get; set; }
    }
}

