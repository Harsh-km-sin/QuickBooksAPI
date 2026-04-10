namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Customer row returned by read APIs; maps from persistence (<see cref="DataAccessLayer.Models.Customer"/>).
/// </summary>
public sealed class CustomerDto
{
    public int Id { get; set; }
    public string QboId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public decimal Balance { get; set; }
    public string PrimaryEmailAddr { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public string BillAddrLine1 { get; set; } = string.Empty;
    public string BillAddrCity { get; set; } = string.Empty;
    public string BillAddrPostalCode { get; set; } = string.Empty;
    public string BillAddrCountrySubDivisionCode { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool Sparse { get; set; }
}
