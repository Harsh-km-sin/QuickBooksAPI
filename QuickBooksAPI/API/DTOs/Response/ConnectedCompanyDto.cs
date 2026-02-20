namespace QuickBooksAPI.API.DTOs.Response
{
    public class ConnectedCompanyDto
    {
        public int Id { get; set; }
        public string QboRealmId { get; set; } = null!;
        public string? CompanyName { get; set; }
        public DateTimeOffset? ConnectedAtUtc { get; set; }
        public bool IsQboConnected { get; set; }
    }
}

