using System.Globalization;
using System.Text.Json;
using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.DataAccessLayer.DTOs;

namespace QuickBooksAPI.Services.Reports;

/// <summary>
/// Flattens a QBO report response (a recursive Header/Columns/Rows tree) into the relational shape
/// stored by <c>dbo.UpsertReportRun</c>.
///
/// Shapes in the real payload that this has to tolerate (all confirmed against a captured sandbox
/// response — see docs/REPORTS_SYNC_QBO_API_RESEARCH.md §4.1):
///  - Nesting is arbitrary depth, not two levels.
///  - "group" appears on top-level sections ONLY; nested sub-sections have no group.
///  - Summary-only virtual sections (GrossProfit, NetOperatingIncome, NetIncome) have no Header
///    and no Rows. That is normal, not an error.
///  - A section's Header row can itself carry amounts — a parent account with its own postings,
///    distinct from the section subtotal. Both are kept.
///  - Cells can be empty strings, and amounts can be negative.
/// </summary>
internal static class QuickBooksReportMapper
{
    private const string PathSeparator = "|";

    /// <summary>Suffix marking a parent account's own postings, kept distinct from its subtotal.</summary>
    private const string OwnPostingsSuffix = "(own)";

    /// <summary>
    /// Must match dbo.QBOReportRow.RowPath. Capped at 800 so (ReportRunId + RowPath) fits SQL
    /// Server's 1700-byte index key limit; real QBO account paths are far shorter.
    /// </summary>
    private const int MaxRowPathLength = 800;

    public static FlattenedReport Flatten(
        string json,
        int userId,
        string realmId,
        string reportType,
        string granularity,
        string accountingMethod,
        DateTime periodStart,
        DateTime periodEnd)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Report JSON cannot be null or empty.", nameof(json));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var result = new FlattenedReport
        {
            Run = new ReportRunUpsertRow
            {
                UserId = userId,
                RealmId = realmId,
                ReportType = reportType,
                Granularity = granularity,
                AccountingMethod = accountingMethod,
                ValueSemantics = ReportValueSemantics.For(reportType),
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Currency = ReadHeaderString(root, "Currency"),
                NoReportData = ReadNoReportData(root),
                GeneratedAtUtc = ReadHeaderTime(root),
                RawJson = json
            }
        };

        result.Columns.AddRange(ReadColumns(root, periodStart));

        var rowNumber = 0;
        if (root.TryGetProperty("Rows", out var rowsEl) &&
            rowsEl.TryGetProperty("Row", out var rowArray) &&
            rowArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rowArray.EnumerateArray())
                WalkRow(row, parentPath: null, parentRowNumber: null, depth: 0, result, ref rowNumber);
        }

        return result;
    }

    // ---------------------------------------------------------------- header

    private static string? ReadHeaderString(JsonElement root, string propertyName) =>
        root.TryGetProperty("Header", out var header) &&
        header.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset? ReadHeaderTime(JsonElement root)
    {
        var time = ReadHeaderString(root, "Time");
        return DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// QBO signals an empty period through <c>Header.Option[NoReportData]</c> rather than an error
    /// or an empty body, so a zero-activity month still returns a full account skeleton.
    /// </summary>
    private static bool ReadNoReportData(JsonElement root)
    {
        if (!root.TryGetProperty("Header", out var header) ||
            !header.TryGetProperty("Option", out var options) ||
            options.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var option in options.EnumerateArray())
        {
            if (option.TryGetProperty("Name", out var name) &&
                string.Equals(name.GetString(), "NoReportData", StringComparison.OrdinalIgnoreCase) &&
                option.TryGetProperty("Value", out var value))
            {
                return string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    // --------------------------------------------------------------- columns

    /// <summary>
    /// Column periods come from each column's own MetaData StartDate/EndDate, which QBO emits for
    /// summarized columns. That is machine data; ColTitle ("Jan 2024") is a display string and is
    /// never parsed. Columns without those keys — the leading Account column and QBO's grand-total
    /// column — get a null period and are excluded from the read views, so the total column can
    /// never be double-counted alongside the months it already sums.
    ///
    /// The ordinal fallback exists only for the case where MetaData is absent entirely; it assumes
    /// consecutive months from the run's start, which is what summarize_column_by=Month returns.
    /// </summary>
    private static IEnumerable<ReportColumnUpsertRow> ReadColumns(JsonElement root, DateTime periodStart)
    {
        var columns = new List<ReportColumnUpsertRow>();

        if (!root.TryGetProperty("Columns", out var columnsEl) ||
            !columnsEl.TryGetProperty("Column", out var columnArray) ||
            columnArray.ValueKind != JsonValueKind.Array)
        {
            return columns;
        }

        var columnNumber = 0;
        var monthOrdinal = 0;

        foreach (var column in columnArray.EnumerateArray())
        {
            var colType = column.TryGetProperty("ColType", out var ct) ? ct.GetString() : null;
            var colTitle = column.TryGetProperty("ColTitle", out var title) ? title.GetString() : null;

            var metadata = ReadColumnMetadata(column);
            metadata.TryGetValue("ColKey", out var colKey);
            metadata.TryGetValue("StartDate", out var startRaw);
            metadata.TryGetValue("EndDate", out var endRaw);

            DateTime? colPeriodStart = ParseDate(startRaw);
            DateTime? colPeriodEnd = ParseDate(endRaw);

            var isMoneyColumn = string.Equals(colType, "Money", StringComparison.OrdinalIgnoreCase);
            var isTotalColumn = string.Equals(colKey, "total", StringComparison.OrdinalIgnoreCase);

            if (colPeriodStart is null && colPeriodEnd is null && isMoneyColumn && !isTotalColumn)
            {
                var monthStart = periodStart.AddMonths(monthOrdinal);
                colPeriodStart = monthStart;
                colPeriodEnd = monthStart.AddMonths(1).AddDays(-1);
            }

            if (colPeriodStart is not null)
                monthOrdinal++;

            columns.Add(new ReportColumnUpsertRow
            {
                ColumnNumber = columnNumber,
                ColKey = colKey,
                ColTitle = colTitle,
                ColType = colType,
                ColPeriodStart = colPeriodStart,
                ColPeriodEnd = colPeriodEnd
            });

            columnNumber++;
        }

        return columns;
    }

    private static Dictionary<string, string?> ReadColumnMetadata(JsonElement column)
    {
        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!column.TryGetProperty("MetaData", out var metaArray) ||
            metaArray.ValueKind != JsonValueKind.Array)
        {
            return metadata;
        }

        foreach (var entry in metaArray.EnumerateArray())
        {
            if (entry.TryGetProperty("Name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                var key = name.GetString();
                if (!string.IsNullOrEmpty(key))
                    metadata[key] = entry.TryGetProperty("Value", out var value) ? value.GetString() : null;
            }
        }

        return metadata;
    }

    // ------------------------------------------------------------------ rows

    private static void WalkRow(
        JsonElement row,
        string? parentPath,
        int? parentRowNumber,
        int depth,
        FlattenedReport result,
        ref int rowNumber)
    {
        var hasColData = row.TryGetProperty("ColData", out var colData) && colData.ValueKind == JsonValueKind.Array;
        var hasHeader = row.TryGetProperty("Header", out var header);
        var hasSummary = row.TryGetProperty("Summary", out var summary);
        var hasChildren = row.TryGetProperty("Rows", out var childRows) &&
                          childRows.TryGetProperty("Row", out var childArray) &&
                          childArray.ValueKind == JsonValueKind.Array;

        // "type" is not always present; a flat ColData array means a leaf regardless.
        var declaredType = row.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
        var isLeaf = hasColData && !hasHeader && !hasSummary && !hasChildren;

        if (isLeaf || string.Equals(declaredType, "Data", StringComparison.OrdinalIgnoreCase) && hasColData)
        {
            EmitRow(
                result,
                ref rowNumber,
                parentPath,
                parentRowNumber,
                depth,
                rowType: "Data",
                groupName: null,
                labelSource: colData,
                amountSource: colData,
                isSummaryRow: false,
                pathSuffix: null);
            return;
        }

        // Section. Its own amount is the Summary subtotal; the Header (when it carries amounts) is
        // the parent account's own postings and is emitted separately as a child.
        var groupName = row.TryGetProperty("group", out var groupEl) ? groupEl.GetString() : null;
        var labelSource = hasHeader && header.TryGetProperty("ColData", out var headerColData)
            ? headerColData
            : (hasSummary && summary.TryGetProperty("ColData", out var summaryColData) ? summaryColData : default);
        var amountSource = hasSummary && summary.TryGetProperty("ColData", out var summaryAmounts)
            ? summaryAmounts
            : default;

        var sectionLabel = ReadFirstValue(labelSource);
        var sectionPath = BuildPath(parentPath, groupName ?? sectionLabel ?? $"section-{rowNumber}");
        var sectionRowNumber = rowNumber;

        EmitRowAtPath(
            result,
            ref rowNumber,
            path: sectionPath,
            parentPath: parentPath,
            parentRowNumber: parentRowNumber,
            depth: depth,
            rowType: "Section",
            groupName: groupName,
            label: sectionLabel,
            accountQboId: ReadFirstId(labelSource),
            amountSource: amountSource,
            isSummaryRow: true);

        // A parent account with its own direct postings: keep them, or they vanish from the totals.
        if (hasHeader &&
            header.TryGetProperty("ColData", out var ownColData) &&
            HasAnyAmount(ownColData))
        {
            EmitRowAtPath(
                result,
                ref rowNumber,
                path: BuildPath(sectionPath, OwnPostingsSuffix),
                parentPath: sectionPath,
                parentRowNumber: sectionRowNumber,
                depth: depth + 1,
                rowType: "Data",
                groupName: null,
                label: sectionLabel,
                accountQboId: ReadFirstId(ownColData),
                amountSource: ownColData,
                isSummaryRow: false);
        }

        if (hasChildren)
        {
            foreach (var child in childRows.GetProperty("Row").EnumerateArray())
                WalkRow(child, sectionPath, sectionRowNumber, depth + 1, result, ref rowNumber);
        }
    }

    private static void EmitRow(
        FlattenedReport result,
        ref int rowNumber,
        string? parentPath,
        int? parentRowNumber,
        int depth,
        string rowType,
        string? groupName,
        JsonElement labelSource,
        JsonElement amountSource,
        bool isSummaryRow,
        string? pathSuffix)
    {
        var label = ReadFirstValue(labelSource);
        var path = BuildPath(parentPath, pathSuffix ?? label ?? $"row-{rowNumber}");

        EmitRowAtPath(
            result,
            ref rowNumber,
            path,
            parentPath,
            parentRowNumber,
            depth,
            rowType,
            groupName,
            label,
            ReadFirstId(labelSource),
            amountSource,
            isSummaryRow);
    }

    private static void EmitRowAtPath(
        FlattenedReport result,
        ref int rowNumber,
        string path,
        string? parentPath,
        int? parentRowNumber,
        int depth,
        string rowType,
        string? groupName,
        string? label,
        string? accountQboId,
        JsonElement amountSource,
        bool isSummaryRow)
    {
        var currentRowNumber = rowNumber;

        result.Rows.Add(new ReportRowUpsertRow
        {
            RowNumber = currentRowNumber,
            ParentRowNumber = parentRowNumber,
            Depth = depth,
            RowType = rowType,
            GroupName = groupName,
            Label = Truncate(label, 500),
            AccountQboId = Truncate(accountQboId, 50),
            IsSummaryRow = isSummaryRow,
            // 800 matches dbo.QBOReportRow.RowPath, sized to fit the index key limit.
            RowPath = Truncate(path, MaxRowPathLength)!,
            ParentRowPath = Truncate(parentPath, MaxRowPathLength)
        });

        if (amountSource.ValueKind == JsonValueKind.Array)
        {
            var columnNumber = 0;
            foreach (var cell in amountSource.EnumerateArray())
            {
                // Column 0 is the account/label column — never a money cell.
                if (columnNumber > 0)
                {
                    var raw = cell.TryGetProperty("value", out var valueEl) ? valueEl.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        result.Values.Add(new ReportRowColumnValueUpsertRow
                        {
                            RowNumber = currentRowNumber,
                            ColumnNumber = columnNumber,
                            Amount = ParseAmount(raw),
                            RawValue = Truncate(raw, 255)
                        });
                    }
                }

                columnNumber++;
            }
        }

        rowNumber++;
    }

    // ----------------------------------------------------------------- utils

    private static bool HasAnyAmount(JsonElement colData)
    {
        if (colData.ValueKind != JsonValueKind.Array)
            return false;

        var index = 0;
        foreach (var cell in colData.EnumerateArray())
        {
            if (index > 0 &&
                cell.TryGetProperty("value", out var value) &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    private static string? ReadFirstValue(JsonElement colData)
    {
        if (colData.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var cell in colData.EnumerateArray())
            return cell.TryGetProperty("value", out var value) ? value.GetString() : null;

        return null;
    }

    private static string? ReadFirstId(JsonElement colData)
    {
        if (colData.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var cell in colData.EnumerateArray())
            return cell.TryGetProperty("id", out var id) ? id.GetString() : null;

        return null;
    }

    private static string BuildPath(string? parentPath, string segment)
    {
        var safeSegment = segment.Replace(PathSeparator, "/");
        return string.IsNullOrEmpty(parentPath) ? safeSegment : $"{parentPath}{PathSeparator}{safeSegment}";
    }

    private static decimal? ParseAmount(string? raw) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static DateTime? ParseDate(string? raw) =>
        DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
