using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Mapping;

internal static class VendorCustomerReadMapping
{
    public static VendorDto ToDto(Vendor v) => new()
    {
        Id = v.Id,
        QboId = v.QboId,
        UserId = v.UserId,
        RealmId = v.RealmId,
        SyncToken = v.SyncToken,
        Title = v.Title,
        GivenName = v.GivenName,
        MiddleName = v.MiddleName,
        FamilyName = v.FamilyName,
        Suffix = v.Suffix,
        DisplayName = v.DisplayName,
        CompanyName = v.CompanyName,
        PrintOnCheckName = v.PrintOnCheckName,
        Active = v.Active,
        Balance = v.Balance,
        PrimaryEmailAddr = v.PrimaryEmailAddr,
        PrimaryPhone = v.PrimaryPhone,
        Mobile = v.Mobile,
        WebAddr = v.WebAddr,
        TaxIdentifier = v.TaxIdentifier,
        AcctNum = v.AcctNum,
        BillAddrLine1 = v.BillAddrLine1,
        BillAddrLine2 = v.BillAddrLine2,
        BillAddrLine3 = v.BillAddrLine3,
        BillAddrCity = v.BillAddrCity,
        BillAddrPostalCode = v.BillAddrPostalCode,
        BillAddrCountrySubDivisionCode = v.BillAddrCountrySubDivisionCode,
        BillAddrCountry = v.BillAddrCountry,
        Domain = v.Domain,
        Sparse = v.Sparse,
        CreateTime = v.CreateTime,
        LastUpdatedTime = v.LastUpdatedTime,
        DeletedAt = v.DeletedAt,
        DeletedBy = v.DeletedBy
    };

    public static CustomerDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        QboId = c.QboId,
        UserId = c.UserId,
        RealmId = c.RealmId,
        SyncToken = c.SyncToken,
        Title = c.Title,
        GivenName = c.GivenName,
        MiddleName = c.MiddleName,
        FamilyName = c.FamilyName,
        DisplayName = c.DisplayName,
        CompanyName = c.CompanyName,
        Active = c.Active,
        Balance = c.Balance,
        PrimaryEmailAddr = c.PrimaryEmailAddr,
        PrimaryPhone = c.PrimaryPhone,
        BillAddrLine1 = c.BillAddrLine1,
        BillAddrCity = c.BillAddrCity,
        BillAddrPostalCode = c.BillAddrPostalCode,
        BillAddrCountrySubDivisionCode = c.BillAddrCountrySubDivisionCode,
        CreateTime = c.CreateTime,
        LastUpdatedTime = c.LastUpdatedTime,
        Domain = c.Domain,
        Sparse = c.Sparse
    };

    public static PagedResult<VendorDto> ToVendorDtoPaged(PagedResult<Vendor> paged)
    {
        return new PagedResult<VendorDto>
        {
            Items = paged.Items.Select(ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public static PagedResult<CustomerDto> ToCustomerDtoPaged(PagedResult<Customer> paged)
    {
        return new PagedResult<CustomerDto>
        {
            Items = paged.Items.Select(ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
