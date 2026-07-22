-- Read layer for stored QBO reports.
-- NOTE: Run this script manually against the application database, AFTER CreateReportTables.sql.
--
-- WHY VIEWS + PROCS RATHER THAN ONE GENERIC PROC
--
-- Splitting the read path per report type lets each one bake in its own aggregation rule, so there
-- is no runtime branch on ValueSemantics that could be applied to the wrong report:
--
--   P&L values are FLOW  - additive. A quarter is Jan + Feb + Mar.
--   BS  values are STOCK - point-in-time. A quarter is the Mar 31 column ALONE.
--
-- Summing balance-sheet columns yields plausible-looking, badly wrong numbers (roughly 3x total
-- assets for a quarter). Keeping the two paths physically separate is what makes that unrepresentable.
--
-- A view cannot take parameters and both reports need a user-selected date range, so the split is:
-- views do the joining and shaping, procs apply the range and the per-type aggregation. The API
-- layer only re-nests rows into a tree; it performs no arithmetic.

-- ---------------------------------------------------------------------------
-- Base view: joins the four tables into flat, clean line-level rows.
-- No aggregation and no report-specific logic lives here.
-- ---------------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_ReportLines
AS
SELECT
    run.Id               AS ReportRunId,
    run.UserId,
    run.RealmId,
    run.ReportType,
    run.Granularity,
    run.AccountingMethod,
    run.ValueSemantics,
    run.Currency,
    run.SyncedAtUtc,
    rw.RowNumber,
    rw.ParentRowNumber,
    rw.Depth,
    rw.RowType,
    rw.GroupName,
    rw.Label,
    rw.AccountQboId,
    rw.IsSummaryRow,
    rw.RowPath,
    rw.ParentRowPath,
    col.ColumnNumber,
    col.ColKey,
    col.ColTitle,
    col.ColPeriodStart,
    col.ColPeriodEnd,
    val.Amount,
    val.RawValue
FROM dbo.QBOReportRun run
INNER JOIN dbo.QBOReportRow rw
    ON rw.ReportRunId = run.Id
INNER JOIN dbo.QBOReportColumn col
    ON col.ReportRunId = run.Id
LEFT JOIN dbo.QBOReportRowColumnValue val
    ON  val.ReportRunId  = run.Id
    AND val.RowNumber    = rw.RowNumber
    AND val.ColumnNumber = col.ColumnNumber
-- Excludes the leading account/label column, which carries no period and no money.
WHERE col.ColPeriodStart IS NOT NULL
  AND col.ColPeriodEnd   IS NOT NULL;
GO

CREATE OR ALTER VIEW dbo.vw_ProfitAndLossLines
AS
SELECT * FROM dbo.vw_ReportLines WHERE ReportType = 'ProfitAndLoss';
GO

CREATE OR ALTER VIEW dbo.vw_BalanceSheetLines
AS
SELECT * FROM dbo.vw_ReportLines WHERE ReportType = 'BalanceSheet';
GO

-- ---------------------------------------------------------------------------
-- P&L: SUM every monthly column that falls inside the requested range.
--
-- Grouping is by RowPath, not RowNumber: RowNumber is only unique within a run, and a range
-- spanning a fiscal-year boundary reads from two runs. RowPath is stable across runs.
--
-- The ROW_NUMBER() de-duplication guards against overlapping runs covering the same month
-- (possible if chunk boundaries ever shift, e.g. after a fiscal-year change). Without it the
-- overlapping month would be counted twice; with it, the most recently synced run wins.
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetProfitAndLossTree
    @UserId           INT,
    @RealmId          NVARCHAR(50),
    @RangeStart       DATE,
    @RangeEnd         DATE,
    @AccountingMethod NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    WITH Deduped AS
    (
        SELECT
            l.*,
            ROW_NUMBER() OVER (
                PARTITION BY l.RowPath, l.ColPeriodStart, l.ColPeriodEnd
                ORDER BY l.SyncedAtUtc DESC, l.ReportRunId DESC
            ) AS DedupeRank
        FROM dbo.vw_ProfitAndLossLines l
        WHERE l.UserId           = @UserId
          AND l.RealmId          = @RealmId
          AND l.AccountingMethod = @AccountingMethod
          AND l.ColPeriodStart  >= @RangeStart
          AND l.ColPeriodEnd    <= @RangeEnd
    )
    SELECT
        MIN(RowNumber)       AS RowNumber,
        RowPath,
        ParentRowPath,
        MIN(Depth)           AS Depth,
        MIN(RowType)         AS RowType,
        MIN(GroupName)       AS GroupName,
        MIN(Label)           AS Label,
        MIN(AccountQboId)    AS AccountQboId,
        MAX(CAST(IsSummaryRow AS INT)) AS IsSummaryRow,
        SUM(Amount)          AS Amount
    FROM Deduped
    WHERE DedupeRank = 1
    GROUP BY RowPath, ParentRowPath
    ORDER BY MIN(RowNumber);
END;
GO

-- ---------------------------------------------------------------------------
-- Balance Sheet: pick the single latest month-end column at or before the range end.
-- Never sums. @RangeStart is accepted only so both procs share a call shape; it is
-- intentionally unused, since a balance sheet is an as-of snapshot.
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetBalanceSheetTree
    @UserId           INT,
    @RealmId          NVARCHAR(50),
    @RangeStart       DATE,
    @RangeEnd         DATE,
    @AccountingMethod NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfPeriodEnd DATE =
    (
        SELECT MAX(l.ColPeriodEnd)
        FROM dbo.vw_BalanceSheetLines l
        WHERE l.UserId           = @UserId
          AND l.RealmId          = @RealmId
          AND l.AccountingMethod = @AccountingMethod
          AND l.ColPeriodEnd    <= @RangeEnd
    );

    IF @AsOfPeriodEnd IS NULL
    BEGIN
        -- No stored snapshot at or before the requested date; return an empty result set
        -- with the same shape so callers need no special case.
        SELECT
            CAST(NULL AS INT)            AS RowNumber,
            CAST(NULL AS NVARCHAR(800))  AS RowPath,
            CAST(NULL AS NVARCHAR(800))  AS ParentRowPath,
            CAST(NULL AS INT)            AS Depth,
            CAST(NULL AS NVARCHAR(20))   AS RowType,
            CAST(NULL AS NVARCHAR(100))  AS GroupName,
            CAST(NULL AS NVARCHAR(500))  AS Label,
            CAST(NULL AS NVARCHAR(50))   AS AccountQboId,
            CAST(NULL AS INT)            AS IsSummaryRow,
            CAST(NULL AS DECIMAL(18,2))  AS Amount
        WHERE 1 = 0;
        RETURN;
    END;

    -- Leading semicolon: T-SQL requires the statement preceding a CTE to be terminated, and the
    -- END above closes a block rather than a statement.
    ;WITH Deduped AS
    (
        SELECT
            l.*,
            ROW_NUMBER() OVER (
                PARTITION BY l.RowPath
                ORDER BY l.SyncedAtUtc DESC, l.ReportRunId DESC
            ) AS DedupeRank
        FROM dbo.vw_BalanceSheetLines l
        WHERE l.UserId           = @UserId
          AND l.RealmId          = @RealmId
          AND l.AccountingMethod = @AccountingMethod
          AND l.ColPeriodEnd     = @AsOfPeriodEnd
    )
    SELECT
        RowNumber,
        RowPath,
        ParentRowPath,
        Depth,
        RowType,
        GroupName,
        Label,
        AccountQboId,
        CAST(IsSummaryRow AS INT) AS IsSummaryRow,
        Amount
    FROM Deduped
    WHERE DedupeRank = 1
    ORDER BY RowNumber;
END;
GO
