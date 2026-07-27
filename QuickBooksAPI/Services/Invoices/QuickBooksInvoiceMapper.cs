using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;

namespace QuickBooksAPI.Services.Invoices;

public static class QuickBooksInvoiceMapper
{
    public static QBOInvoiceHeader MapToHeader(QuickBooksInvoiceDto inv, string realmId)
    {
        DateTime txnDate = DateTime.MinValue;
        DateTime dueDate = DateTime.MinValue;

        if (!string.IsNullOrWhiteSpace(inv.TxnDate) &&
            DateTime.TryParseExact(
                inv.TxnDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTxnDate))
            txnDate = parsedTxnDate;

        if (!string.IsNullOrWhiteSpace(inv.DueDate) &&
            DateTime.TryParseExact(
                inv.DueDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDueDate))
            dueDate = parsedDueDate;
        else
            dueDate = txnDate;

        return new QBOInvoiceHeader
        {
            QBOInvoiceId = inv.QBOId,
            RealmId = realmId,
            SyncToken = inv.SyncToken,
            Domain = inv.Domain,
            Sparse = inv.Sparse,

            TxnDate = txnDate,
            DueDate = dueDate,

            CustomerRefId = inv.CustomerRef?.Value,
            CustomerRefName = inv.CustomerRef?.Name,

            CurrencyCode = inv.CurrencyRef?.Value,
            ExchangeRate = inv.ExchangeRate ?? 1m,

            TotalAmt = inv.TotalAmt,
            Balance = inv.Balance,

            PrivateNote = inv.PrivateNote,
            CustomerMemo = inv.CustomerMemo?.Value,
            SalesTermRefId = inv.SalesTermRef?.Value,

            CreateTime = inv.MetaData?.CreateTime ?? DateTimeOffset.UtcNow,
            LastUpdatedTime = inv.MetaData?.LastUpdatedTime ?? DateTimeOffset.UtcNow,

            RawJson = JsonSerializer.Serialize(
                inv,
                new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
        };
    }

    public static List<InvoiceLineUpsertRow> MapToLineUpsertRows(QuickBooksInvoiceDto inv, string realmId)
    {
        var list = new List<InvoiceLineUpsertRow>();
        if (inv?.Line == null || inv.Line.Count == 0)
            return list;

        for (var i = 0; i < inv.Line.Count; i++)
        {
            var line = inv.Line[i];
            var detail = line.SalesItemLineDetail;
            list.Add(new InvoiceLineUpsertRow
            {
                QBOInvoiceId = inv.QBOId,
                RealmId = realmId,
                QBLineId = line.Id,
                LineNum = line.LineNum ?? i,
                DetailType = line.DetailType,
                Description = line.Description,
                Amount = line.Amount,
                ItemRefId = detail?.ItemRef?.Value,
                ItemRefName = detail?.ItemRef?.Name,
                Qty = detail?.Qty,
                UnitPrice = detail?.UnitPrice,
                TaxCodeRef = detail?.TaxCodeRef?.Value,
                RawLineJson = JsonSerializer.Serialize(
                    line,
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
            });
        }

        return list;
    }
}
