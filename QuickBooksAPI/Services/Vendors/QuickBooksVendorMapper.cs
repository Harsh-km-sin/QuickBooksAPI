using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Services.Vendors;

public static class QuickBooksVendorMapper
{
    public static Vendor Map(QuickBooksVendorQueryDto dto, int userId, string realmId)
    {
        return new Vendor
        {
            QboId = dto.Id,
            UserId = userId.ToString(),
            RealmId = realmId,
            SyncToken = dto.SyncToken,
            Title = dto.Title,
            GivenName = dto.GivenName,
            MiddleName = dto.MiddleName,
            FamilyName = dto.FamilyName,
            Suffix = dto.Suffix,
            DisplayName = dto.DisplayName,
            CompanyName = dto.CompanyName,
            PrintOnCheckName = dto.PrintOnCheckName,
            Active = dto.Active,
            Balance = dto.Balance,
            PrimaryEmailAddr = dto.PrimaryEmailAddr?.Address,
            PrimaryPhone = dto.PrimaryPhone?.FreeFormNumber,
            Mobile = dto.Mobile?.FreeFormNumber,
            WebAddr = dto.WebAddr?.URI,
            TaxIdentifier = dto.TaxIdentifier,
            AcctNum = dto.AcctNum,
            BillAddrLine1 = dto.BillAddr?.Line1,
            BillAddrLine2 = dto.BillAddr?.Line2,
            BillAddrLine3 = dto.BillAddr?.Line3,
            BillAddrCity = dto.BillAddr?.City,
            BillAddrPostalCode = dto.BillAddr?.PostalCode,
            BillAddrCountrySubDivisionCode = dto.BillAddr?.CountrySubDivisionCode,
            BillAddrCountry = dto.BillAddr?.Country,
            Domain = dto.Domain,
            Sparse = dto.Sparse,
            CreateTime = dto.MetaData?.CreateTime != null
                ? new DateTimeOffset(dto.MetaData.CreateTime.ToUniversalTime(), TimeSpan.Zero)
                : DateTimeOffset.UtcNow,
            LastUpdatedTime = dto.MetaData?.LastUpdatedTime != null
                ? new DateTimeOffset(dto.MetaData.LastUpdatedTime.ToUniversalTime(), TimeSpan.Zero)
                : DateTimeOffset.UtcNow
        };
    }
}
