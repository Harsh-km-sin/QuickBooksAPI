using System;

namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QBOTerm
    {
        public long TermId { get; set; }
        public string QBOTermId { get; set; } = null!;
        public string RealmId { get; set; } = null!;
        public string SyncToken { get; set; }
        public string Name { get; set; } = null!;
        public bool Active { get; set; } = true;
        public string Type { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? DiscountDays { get; set; }
        public int? DueDays { get; set; }
        public int? DayOfMonthDue { get; set; }
        public int? DueNextMonthDays { get; set; }
        public DateTimeOffset? CreateTime { get; set; }
        public DateTimeOffset? LastUpdatedTime { get; set; }
        public string RawJson { get; set; }
    }
}
