using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using JournalEntryDto = QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs.JournalEntry;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBooksAPI.Features.JournalEntries.Mapping;

internal static class JournalEntrySyncMapper
{
    public static QBOJournalEntryHeader MapToHeader(JournalEntryDto je, string realmId)
    {
        DateTime? txnDate = null;

        if (!string.IsNullOrWhiteSpace(je.TxnDate) &&
            DateTime.TryParseExact(
                je.TxnDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            txnDate = parsedDate;
        }

        return new QBOJournalEntryHeader
        {
            QBJournalEntryId = je.Id,
            QBRealmId = realmId,
            SyncToken = je.SyncToken,
            Domain = je.Domain,

            TxnDate = txnDate,

            Sparse = je.Sparse,
            Adjustment = je.Adjustment,

            DocNumber = je.DocNumber,
            PrivateNote = je.PrivateNote,

            CurrencyCode = je.CurrencyRef?.Value,
            ExchangeRate = je.ExchangeRate,

            TotalAmount = je.TotalAmt,
            HomeTotalAmount = je.HomeTotalAmt,

            CreateTime = je.MetaData?.CreateTime,
            LastUpdatedTime = je.MetaData?.LastUpdatedTime,

            RawJson = JsonSerializer.Serialize(
                je,
                new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
        };
    }

    public static IEnumerable<QBOJournalEntryLine> MapToLines(JournalEntryDto je, long journalEntryId)
    {
        if (je?.Line == null || je.Line.Count == 0)
            yield break;

        for (var i = 0; i < je.Line.Count; i++)
        {
            var line = je.Line[i];
            var detail = line.JournalEntryLineDetail;

            yield return new QBOJournalEntryLine
            {
                JournalEntryId = journalEntryId,
                QBLineId = line.Id,
                LineNum = i,

                DetailType = line.DetailType,
                Description = line.Description,

                Amount = line.Amount,

                PostingType = detail?.PostingType,

                AccountRefId = detail?.AccountRef?.Value,
                AccountRefName = detail?.AccountRef?.Name,

                EntityType = detail?.Entity?.Type,
                EntityRefId = detail?.Entity?.Ref?.Value,
                EntityRefName = detail?.Entity?.Ref?.Name,

                RawLineJson = JsonSerializer.Serialize(
                    line,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })
            };
        }
    }
}
