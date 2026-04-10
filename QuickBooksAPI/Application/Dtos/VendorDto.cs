namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// Vendor row returned by read APIs; maps from persistence (<see cref="DataAccessLayer.Models.Vendor"/>).
/// </summary>
public sealed class VendorDto
{
    public int Id { get; set; }
    public string QboId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? GivenName { get; set; }
    public string? MiddleName { get; set; }
    public string? FamilyName { get; set; }
    public string? Suffix { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? PrintOnCheckName { get; set; }
    public bool Active { get; set; }
    public decimal Balance { get; set; }
    public string? PrimaryEmailAddr { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? Mobile { get; set; }
    public string? WebAddr { get; set; }
    public string? TaxIdentifier { get; set; }
    public string? AcctNum { get; set; }
    public string? BillAddrLine1 { get; set; }
    public string? BillAddrLine2 { get; set; }
    public string? BillAddrLine3 { get; set; }
    public string? BillAddrCity { get; set; }
    public string? BillAddrPostalCode { get; set; }
    public string? BillAddrCountrySubDivisionCode { get; set; }
    public string? BillAddrCountry { get; set; }
    public string? Domain { get; set; }
    public bool Sparse { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset LastUpdatedTime { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
