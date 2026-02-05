namespace QuickBooksAPI.DataAccessLayer.Models
{
    /// <summary>
    /// DB model for Bill line table. Maps from QuickBooks Bill.Line (expense line detail).
    /// </summary>
    public class QBOBillLine
    {
        // Local DB PK
        public long BillLineId { get; set; }

        // FK to Bill header
        public long BillId { get; set; }

        // QBO line identity
        public string? QBLineId { get; set; }
        public int? LineNum { get; set; }

        public string? Description { get; set; }
        public string? DetailType { get; set; }
        public decimal Amount { get; set; }

        // ProjectRef
        public string? ProjectRefValue { get; set; }

        // AccountBasedExpenseLineDetail
        public string? AccountRefValue { get; set; }
        public string? AccountRefName { get; set; }
        public string? TaxCodeRefValue { get; set; }
        public string? BillableStatus { get; set; }
        public string? CustomerRefValue { get; set; }
        public string? CustomerRefName { get; set; }

        // ItemBasedExpenseLineDetail (if used)
        public string? ItemRefValue { get; set; }
        public string? ItemRefName { get; set; }
        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }

        // Raw line JSON
        public string? RawLineJson { get; set; }
    }
}
