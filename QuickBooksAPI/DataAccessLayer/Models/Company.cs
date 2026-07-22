namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class Company
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string QboRealmId { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? QboAccessToken { get; set; }
        public string? QboRefreshToken { get; set; }
        public DateTimeOffset? TokenExpiryUtc { get; set; }
        public bool IsQboConnected { get; set; }
        public DateTimeOffset? ConnectedAtUtc { get; set; }
        public DateTimeOffset? DisconnectedAtUtc { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; set; }

        /// <summary>"Cash" or "Accrual" — from QBO ReportPrefs.ReportBasis. Every synced report uses this basis.</summary>
        public string? AccountingBasis { get; set; }

        /// <summary>Lower bound for report history backfill. Null means "discover it by walking back".</summary>
        public DateTime? CompanyStartDate { get; set; }

        /// <summary>1-12. Pins the Balance Sheet start_date and drives the fiscal-year read filter.</summary>
        public int? FiscalYearStartMonth { get; set; }
    }
}

