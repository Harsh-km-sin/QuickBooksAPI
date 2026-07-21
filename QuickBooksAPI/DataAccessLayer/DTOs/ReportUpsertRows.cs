namespace QuickBooksAPI.DataAccessLayer.DTOs
{
    /// <summary>
    /// Flat rows fed to the <c>dbo.UpsertReportRun</c> table-valued parameters.
    /// Column order in the repository's DataTable builders must match the TVP definitions in
    /// <c>Scripts/UpsertReport.sql</c> exactly — <c>DataTable.Rows.Add(...)</c> is positional.
    /// </summary>
    public sealed class ReportRunUpsertRow
    {
        public int UserId { get; set; }
        public string RealmId { get; set; } = null!;
        public string ReportType { get; set; } = null!;
        public string Granularity { get; set; } = null!;
        public string AccountingMethod { get; set; } = null!;
        public string ValueSemantics { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? Currency { get; set; }
        public bool NoReportData { get; set; }
        public DateTimeOffset? GeneratedAtUtc { get; set; }
        public string? RawJson { get; set; }
    }

    public sealed class ReportColumnUpsertRow
    {
        public int ColumnNumber { get; set; }
        public string? ColKey { get; set; }
        public string? ColTitle { get; set; }
        public string? ColType { get; set; }
        public DateTime? ColPeriodStart { get; set; }
        public DateTime? ColPeriodEnd { get; set; }
    }

    public sealed class ReportRowUpsertRow
    {
        public int RowNumber { get; set; }
        public int? ParentRowNumber { get; set; }
        public int Depth { get; set; }
        public string RowType { get; set; } = null!;
        public string? GroupName { get; set; }
        public string? Label { get; set; }
        public string? AccountQboId { get; set; }
        public bool IsSummaryRow { get; set; }
        public string RowPath { get; set; } = null!;
        public string? ParentRowPath { get; set; }
    }

    public sealed class ReportRowColumnValueUpsertRow
    {
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? RawValue { get; set; }
    }

    /// <summary>
    /// The mapper's output for one report pull: everything needed for a single
    /// <c>dbo.UpsertReportRun</c> call.
    /// </summary>
    public sealed class FlattenedReport
    {
        public ReportRunUpsertRow Run { get; set; } = null!;
        public List<ReportColumnUpsertRow> Columns { get; set; } = new();
        public List<ReportRowUpsertRow> Rows { get; set; } = new();
        public List<ReportRowColumnValueUpsertRow> Values { get; set; } = new();
    }
}
