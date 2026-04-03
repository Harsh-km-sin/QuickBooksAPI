using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.Services.Customers;

public static class QuickBooksCustomerMapper
{
    public static Customer Map(QuickBooksCustomerDto dto, int userId, string realmId)
    {
        return new Customer
        {
            QboId = dto.QBOId,
            UserId = userId.ToString(),
            RealmId = realmId,
            SyncToken = dto.SyncToken,

            Title = dto.Title,
            GivenName = dto.GivenName,
            MiddleName = dto.MiddleName,
            FamilyName = dto.FamilyName,
            DisplayName = dto.DisplayName,
            CompanyName = dto.CompanyName,
            Active = dto.Active,
            Balance = dto.Balance,
            Domain = dto.Domain,
            Sparse = dto.Sparse,

            PrimaryEmailAddr = dto.PrimaryEmailAddr?.Address,
            PrimaryPhone = dto.PrimaryPhone?.FreeFormNumber,

            BillAddrLine1 = dto.BillAddr?.Line1,
            BillAddrCity = dto.BillAddr?.City,
            BillAddrPostalCode = dto.BillAddr?.PostalCode,
            BillAddrCountrySubDivisionCode = dto.BillAddr?.CountrySubDivisionCode,

            CreateTime = dto.MetaData?.CreateTime ?? DateTime.Now,
            LastUpdatedTime = dto.MetaData?.LastUpdatedTime ?? DateTime.Now
        };
    }
}
