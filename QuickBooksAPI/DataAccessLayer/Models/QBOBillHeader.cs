namespace QuickBooksAPI.DataAccessLayer.Models
{
    /// <summary>
    /// DB model for Bill header table. Maps from QuickBooks Bill (vendor bill / A/P).
    /// </summary>
    public class QBOBillHeader
    {
        // Local DB PK
        public long BillId { get; set; }

        // QBO identity
        public string QBOBillId { get; set; } = null!;
        public string RealmId { get; set; } = null!;
        public string SyncToken { get; set; } = null!;

        public string? Domain { get; set; }
        public bool Sparse { get; set; }

        // AP Account (Accounts Payable)
        public string? APAccountRefValue { get; set; }
        public string? APAccountRefName { get; set; }

        // Vendor
        public string? VendorRefValue { get; set; }
        public string? VendorRefName { get; set; }

        public DateTime? TxnDate { get; set; }
        public DateTime? DueDate { get; set; }

        public decimal TotalAmt { get; set; }
        public decimal Balance { get; set; }
        public bool IsDeleted { get; set; }


        // Currency
        public string? CurrencyRefValue { get; set; }
        public string? CurrencyRefName { get; set; }

        // Terms
        public string? SalesTermRefValue { get; set; }

        // Audit (from MetaData)
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }

        // Optional: full header + LinkedTxn as JSON if needed
        public string? RawJson { get; set; }
    }
}
