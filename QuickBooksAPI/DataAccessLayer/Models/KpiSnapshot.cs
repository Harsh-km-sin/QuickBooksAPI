namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class KpiSnapshot
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public DateTime SnapshotDate { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public decimal KpiValue { get; set; }
        public string Period { get; set; } = "Monthly";
        public string? MetadataJson { get; set; }
    }
}
