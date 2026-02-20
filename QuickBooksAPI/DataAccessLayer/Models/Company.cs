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
    }
}

