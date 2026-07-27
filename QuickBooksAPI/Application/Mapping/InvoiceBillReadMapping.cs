using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Mapping;

internal static class InvoiceBillReadMapping
{
    public static InvoiceListItemDto ToInvoiceDto(QBOInvoiceHeader h) => new()
    {
        InvoiceId = h.InvoiceId,
        QBOInvoiceId = h.QBOInvoiceId,
        RealmId = h.RealmId,
        SyncToken = h.SyncToken,
        Domain = h.Domain,
        Sparse = h.Sparse,
        TxnDate = h.TxnDate,
        DueDate = h.DueDate,
        CurrencyCode = h.CurrencyCode,
        ExchangeRate = h.ExchangeRate,
        CustomerRefId = h.CustomerRefId,
        CustomerRefName = h.CustomerRefName,
        TotalAmt = h.TotalAmt,
        HomeTotalAmt = h.HomeTotalAmt,
        Balance = h.Balance,
        HomeBalance = h.HomeBalance,
        GlobalTaxCalculation = h.GlobalTaxCalculation,
        PrivateNote = h.PrivateNote,
        CustomerMemo = h.CustomerMemo,
        SalesTermRefId = h.SalesTermRefId,
        CreateTime = h.CreateTime,
        LastUpdatedTime = h.LastUpdatedTime,
        RawJson = h.RawJson
    };

    public static BillListItemDto ToBillDto(QBOBillHeader h) => new()
    {
        BillId = h.BillId,
        QBOBillId = h.QBOBillId,
        RealmId = h.RealmId,
        SyncToken = h.SyncToken,
        Domain = h.Domain,
        Sparse = h.Sparse,
        APAccountRefValue = h.APAccountRefValue,
        APAccountRefName = h.APAccountRefName,
        VendorRefValue = h.VendorRefValue,
        VendorRefName = h.VendorRefName,
        TxnDate = h.TxnDate,
        DueDate = h.DueDate,
        TotalAmt = h.TotalAmt,
        Balance = h.Balance,
        IsDeleted = h.IsDeleted,
        CurrencyRefValue = h.CurrencyRefValue,
        CurrencyRefName = h.CurrencyRefName,
        SalesTermRefValue = h.SalesTermRefValue,
        CreateTime = h.CreateTime,
        LastUpdatedTime = h.LastUpdatedTime,
        RawJson = h.RawJson
    };

    public static PagedResult<InvoiceListItemDto> ToInvoiceDtoPaged(PagedResult<QBOInvoiceHeader> paged)
    {
        var items = paged.Items.Select(ToInvoiceDto).ToList();
        return new PagedResult<InvoiceListItemDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public static PagedResult<BillListItemDto> ToBillDtoPaged(PagedResult<QBOBillHeader> paged)
    {
        var items = paged.Items.Select(ToBillDto).ToList();
        return new PagedResult<BillListItemDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
