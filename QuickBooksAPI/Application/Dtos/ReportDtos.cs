namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// One aggregated line as returned by <c>dbo.GetProfitAndLossTree</c> / <c>dbo.GetBalanceSheetTree</c>.
/// Amounts arrive already aggregated — the API layer only re-nests these into a tree and never
/// performs arithmetic on them.
/// </summary>
public sealed class ReportLineRow
{
    public int RowNumber { get; set; }
    public string RowPath { get; set; } = null!;
    public string? ParentRowPath { get; set; }
    public int Depth { get; set; }
    public string RowType { get; set; } = null!;
    public string? GroupName { get; set; }
    public string? Label { get; set; }
    public string? AccountQboId { get; set; }
    public int IsSummaryRow { get; set; }
    public decimal? Amount { get; set; }
}

/// <summary>A node in the report tree returned to the frontend.</summary>
public sealed class ReportNodeDto
{
    public string RowPath { get; set; } = null!;
    public string? Label { get; set; }
    public string RowType { get; set; } = null!;
    public string? GroupName { get; set; }
    public string? AccountQboId { get; set; }
    public int Depth { get; set; }

    /// <summary>True when the amount is a computed subtotal rather than a single account's postings.</summary>
    public bool IsSummary { get; set; }

    public decimal? Amount { get; set; }
    public List<ReportNodeDto> Children { get; set; } = new();
}

/// <summary>A rendered report: the period it covers plus its row tree.</summary>
public sealed class ReportTreeDto
{
    public string ReportType { get; set; } = null!;
    public string AccountingMethod { get; set; } = null!;
    public DateTime RangeStart { get; set; }
    public DateTime RangeEnd { get; set; }

    /// <summary>True for point-in-time reports, where <see cref="RangeStart"/> carries no meaning.</summary>
    public bool IsPointInTime { get; set; }

    public List<ReportNodeDto> Rows { get; set; } = new();
}

/// <summary>One resident period, for bounding the UI's date picker.</summary>
public sealed class ReportPeriodDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string AccountingMethod { get; set; } = null!;
    public string Granularity { get; set; } = null!;
    public bool NoReportData { get; set; }
    public DateTimeOffset SyncedAtUtc { get; set; }
}
