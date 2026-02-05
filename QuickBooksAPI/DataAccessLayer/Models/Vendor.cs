namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class Vendor
    {
        public int Id { get; set; } // Local PK
        public string QboId { get; set; }
        public string UserId { get; set; }
        public string RealmId { get; set; }
        public string SyncToken { get; set; }
        public string Title { get; set; }
        public string GivenName { get; set; }
        public string MiddleName { get; set; }
        public string FamilyName { get; set; }
        public string DisplayName { get; set; }
        public string CompanyName { get; set; }
        public bool Active { get; set; }
        public decimal Balance { get; set; }
        public string PrimaryEmailAddr { get; set; }
        public string PrimaryPhone { get; set; }
        public string BillAddrLine1 { get; set; }
        public string BillAddrCity { get; set; }
        public string BillAddrPostalCode { get; set; }
        public string BillAddrCountrySubDivisionCode { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public string Domain { get; set; }
        public bool Sparse { get; set; }
        /// <summary>Set when vendor is soft-deleted; null means active.</summary>
        public DateTime? DeletedAt { get; set; }
        /// <summary>UserId who performed the soft delete.</summary>
        public string? DeletedBy { get; set; }
    }
}
