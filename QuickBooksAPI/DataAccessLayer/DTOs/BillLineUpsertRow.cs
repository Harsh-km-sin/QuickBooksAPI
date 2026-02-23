namespace QuickBooksAPI.DataAccessLayer.DTOs
{
    /// <summary>
    /// Row for Bill line upsert. Includes QBOBillId + RealmId to link to header.
    /// </summary>
    public class BillLineUpsertRow
    {
        public string QBOBillId { get; set; } = null!;
        public string RealmId { get; set; } = null!;

        public string? QBLineId { get; set; }
        public int? LineNum { get; set; }
        public string? DetailType { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }

        public string? ProjectRefValue { get; set; }

        public string? AccountRefValue { get; set; }
        public string? AccountRefName { get; set; }
        public string? TaxCodeRefValue { get; set; }
        public string? BillableStatus { get; set; }
        public string? CustomerRefValue { get; set; }
        public string? CustomerRefName { get; set; }

        public string? ItemRefValue { get; set; }
        public string? ItemRefName { get; set; }
        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }

        public string? RawLineJson { get; set; }
    }
}
