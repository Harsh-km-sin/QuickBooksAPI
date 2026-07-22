namespace QuickBooksAPI.DataAccessLayer.Models
{
    /// <summary>
    /// One stored QBO report pull. Maps to <c>dbo.QBOReportRun</c>.
    /// Sync chunks history by fiscal year at monthly grain, so a run typically covers one fiscal
    /// year and holds ~12 monthly columns.
    /// </summary>
    public class QBOReportRun
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = null!;
        public string ReportType { get; set; } = null!;
        public string Granularity { get; set; } = null!;
        public string AccountingMethod { get; set; } = null!;

        /// <summary>"Flow" (additive, P&amp;L) or "Stock" (point-in-time, Balance Sheet).</summary>
        public string ValueSemantics { get; set; } = null!;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? Currency { get; set; }
        public bool NoReportData { get; set; }

        /// <summary>QBO's own <c>Header.Time</c> for the pull.</summary>
        public DateTimeOffset? GeneratedAtUtc { get; set; }

        public DateTimeOffset SyncedAtUtc { get; set; }
        public string? RawJson { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }

    /// <summary>One column of a stored report. Maps to <c>dbo.QBOReportColumn</c>.</summary>
    public class QBOReportColumn
    {
        public int Id { get; set; }
        public int ReportRunId { get; set; }
        public int ColumnNumber { get; set; }
        public string? ColKey { get; set; }
        public string? ColTitle { get; set; }
        public string? ColType { get; set; }

        /// <summary>Null for the leading account/label column and for QBO's grand-total column.</summary>
        public DateTime? ColPeriodStart { get; set; }
        public DateTime? ColPeriodEnd { get; set; }
    }

    /// <summary>One node of the report tree. Maps to <c>dbo.QBOReportRow</c>.</summary>
    public class QBOReportRow
    {
        public int Id { get; set; }
        public int ReportRunId { get; set; }

        /// <summary>Pre-order traversal index. Unique within a run only — use RowPath across runs.</summary>
        public int RowNumber { get; set; }

        public int? ParentRowNumber { get; set; }
        public int Depth { get; set; }

        /// <summary>"Data" (leaf account) or "Section" (subtotal node).</summary>
        public string RowType { get; set; } = null!;

        /// <summary>"Income", "Expenses", etc. Present on top-level sections only.</summary>
        public string? GroupName { get; set; }

        public string? Label { get; set; }
        public string? AccountQboId { get; set; }
        public bool IsSummaryRow { get; set; }

        /// <summary>Stable identity across runs, e.g. "Income|Landscaping Services|Job Materials".</summary>
        public string RowPath { get; set; } = null!;

        public string? ParentRowPath { get; set; }
    }

    /// <summary>A single cell. Maps to <c>dbo.QBOReportRowColumnValue</c>.</summary>
    public class QBOReportRowColumnValue
    {
        public long Id { get; set; }
        public int ReportRunId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? RawValue { get; set; }
    }
}
