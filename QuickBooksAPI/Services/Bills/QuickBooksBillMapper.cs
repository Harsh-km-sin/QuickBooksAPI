using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.Services.Bills;

internal static class QuickBooksBillMapper
{
    public static QBOBillHeader MapToHeader(QuickBooksBillDto bill, string realmId)
    {
        DateTime? txnDate = null;
        DateTime? dueDate = null;

        if (!string.IsNullOrWhiteSpace(bill.TxnDate) &&
            DateTime.TryParseExact(bill.TxnDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTxn))
            txnDate = parsedTxn;
        if (!string.IsNullOrWhiteSpace(bill.DueDate) &&
            DateTime.TryParseExact(bill.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDue))
            dueDate = parsedDue;
        else if (txnDate.HasValue)
            dueDate = txnDate;

        return new QBOBillHeader
        {
            QBOBillId = bill.QBOId,
            RealmId = realmId,
            SyncToken = bill.SyncToken ?? "",
            Domain = bill.Domain,
            Sparse = bill.Sparse,
            APAccountRefValue = bill.APAccountRef?.Value,
            APAccountRefName = bill.APAccountRef?.Name,
            VendorRefValue = bill.VendorRef?.Value,
            VendorRefName = bill.VendorRef?.Name,
            TxnDate = txnDate,
            DueDate = dueDate,
            TotalAmt = bill.TotalAmt,
            Balance = bill.Balance,
            CurrencyRefValue = bill.CurrencyRef?.Value,
            CurrencyRefName = bill.CurrencyRef?.Name,
            SalesTermRefValue = bill.SalesTermRef?.Value,
            CreateTime = bill.MetaData != null ? new DateTimeOffset(bill.MetaData.CreateTime.ToUniversalTime()) : DateTimeOffset.UtcNow,
            LastUpdatedTime = bill.MetaData != null ? new DateTimeOffset(bill.MetaData.LastUpdatedTime.ToUniversalTime()) : DateTimeOffset.UtcNow,
            RawJson = JsonSerializer.Serialize(bill, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
        };
    }

    public static List<BillLineUpsertRow> MapToLineUpsertRows(QuickBooksBillDto bill, string realmId)
    {
        var list = new List<BillLineUpsertRow>();
        if (bill.Line == null || bill.Line.Count == 0)
            return list;

        for (var i = 0; i < bill.Line.Count; i++)
        {
            var line = bill.Line[i];
            var accDetail = line.AccountBasedExpenseLineDetail;
            var itemDetail = line.ItemBasedExpenseLineDetail;
            list.Add(new BillLineUpsertRow
            {
                QBOBillId = bill.QBOId,
                RealmId = realmId,
                QBLineId = line.Id,
                LineNum = i,
                DetailType = line.DetailType,
                Description = line.Description,
                Amount = line.Amount,
                ProjectRefValue = line.ProjectRef?.Value,
                AccountRefValue = accDetail?.AccountRef?.Value,
                AccountRefName = accDetail?.AccountRef?.Name,
                TaxCodeRefValue = accDetail?.TaxCodeRef?.Value ?? itemDetail?.TaxCodeRef?.Value,
                BillableStatus = accDetail?.BillableStatus ?? itemDetail?.BillableStatus,
                CustomerRefValue = accDetail?.CustomerRef?.Value,
                CustomerRefName = accDetail?.CustomerRef?.Name,
                ItemRefValue = itemDetail?.ItemRef?.Value,
                ItemRefName = itemDetail?.ItemRef?.Name,
                Qty = itemDetail?.Qty,
                UnitPrice = itemDetail?.UnitPrice,
                RawLineJson = JsonSerializer.Serialize(line, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
            });
        }

        return list;
    }
}
