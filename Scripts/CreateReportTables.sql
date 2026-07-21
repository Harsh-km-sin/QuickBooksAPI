-- QBO Reports storage (ProfitAndLoss, BalanceSheet, and whatever report types come next).
-- NOTE: Run this script manually against the application database.
-- Deploy order: this script, then UpsertReport.sql, then CreateReportViews.sql.
--
-- DESIGN NOTES
--
-- One shared table set with a ReportType discriminator, rather than a table set per report type.
-- Adding a new report type is then a code change, not a schema change.
--
-- A "run" is one stored QBO report pull. Sync chunks history by fiscal year and pulls at monthly
-- grain, so a typical run covers one fiscal year and holds ~12 monthly columns. Re-syncing a
-- period overwrites it in place (upsert on the period key below) - no version history is kept.
--
-- ROW IDENTITY ACROSS RUNS: RowNumber is only unique *within* a run, so it cannot be used to line
-- up the same account across two runs. A date range spanning a fiscal-year boundary reads from two
-- runs, so rows carry a materialized RowPath ('Income|Landscaping Services|Job Materials|Fountains')
-- which IS stable across runs. Aggregation groups by RowPath; RowNumber is only for ordering.

IF OBJECT_ID('dbo.QBOReportRun', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.QBOReportRun
    (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        UserId           INT             NOT NULL,
        RealmId          NVARCHAR(50)    NOT NULL,
        ReportType       NVARCHAR(50)    NOT NULL,  -- 'ProfitAndLoss' | 'BalanceSheet'
        Granularity      NVARCHAR(20)    NOT NULL,  -- 'Month' (summarize_column_by)
        AccountingMethod NVARCHAR(20)    NOT NULL,  -- 'Cash' | 'Accrual'
        -- 'Flow' values are additive across columns (P&L). 'Stock' values are point-in-time and
        -- must never be summed (Balance Sheet). The per-report-type read views encode this, but it
        -- is stored here so the data documents itself and future report types have a generic path.
        ValueSemantics   NVARCHAR(10)    NOT NULL,
        PeriodStart      DATE            NOT NULL,
        PeriodEnd        DATE            NOT NULL,
        Currency         NVARCHAR(10)    NULL,
        NoReportData     BIT             NOT NULL DEFAULT 0,
        GeneratedAtUtc   DATETIMEOFFSET  NULL,      -- QBO Header.Time
        SyncedAtUtc      DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        RawJson          NVARCHAR(MAX)   NULL,      -- audit/replay, independent of the flattening
        CreatedAtUtc     DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAtUtc     DATETIMEOFFSET  NULL,
        CONSTRAINT UQ_QBOReportRun_Period UNIQUE
            (UserId, RealmId, ReportType, Granularity, AccountingMethod, PeriodStart, PeriodEnd)
    );

    CREATE NONCLUSTERED INDEX IX_QBOReportRun_User_Realm_Type_Period
        ON dbo.QBOReportRun (UserId, RealmId, ReportType, PeriodStart, PeriodEnd);
END
ELSE
    PRINT 'dbo.QBOReportRun already exists - skipped.';
GO

IF OBJECT_ID('dbo.QBOReportColumn', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.QBOReportColumn
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ReportRunId     INT           NOT NULL
            REFERENCES dbo.QBOReportRun(Id) ON DELETE CASCADE,
        ColumnNumber    INT           NOT NULL,
        ColKey          NVARCHAR(100) NULL,   -- MetaData ColKey; more stable than ColTitle
        ColTitle        NVARCHAR(255) NULL,   -- display string, e.g. 'Jan 2024'
        ColType         NVARCHAR(50)  NULL,   -- 'Account' | 'Money'
        -- Derived during sync from the request window + column ordinal, NOT by parsing ColTitle.
        -- NULL for the leading account/label column, which carries no period and no money.
        ColPeriodStart  DATE          NULL,
        ColPeriodEnd    DATE          NULL,
        CONSTRAINT UQ_QBOReportColumn_Run_Number UNIQUE (ReportRunId, ColumnNumber)
    );

    CREATE NONCLUSTERED INDEX IX_QBOReportColumn_Run_Period
        ON dbo.QBOReportColumn (ReportRunId, ColPeriodStart, ColPeriodEnd);
END
ELSE
    PRINT 'dbo.QBOReportColumn already exists - skipped.';
GO

IF OBJECT_ID('dbo.QBOReportRow', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.QBOReportRow
    (
        Id              INT            IDENTITY(1,1) PRIMARY KEY,
        ReportRunId     INT            NOT NULL
            REFERENCES dbo.QBOReportRun(Id) ON DELETE CASCADE,
        RowNumber       INT            NOT NULL,  -- pre-order traversal index; ordering only
        ParentRowNumber INT            NULL,
        Depth           INT            NOT NULL,
        RowType         NVARCHAR(20)   NOT NULL,  -- 'Data' (leaf account) | 'Section' (subtotal)
        GroupName       NVARCHAR(100)  NULL,      -- 'Income','Expenses',... top-level sections ONLY
        Label           NVARCHAR(500)  NULL,
        AccountQboId    NVARCHAR(50)   NULL,      -- join key back to dbo.ChartOfAccounts
        IsSummaryRow    BIT            NOT NULL DEFAULT 0,  -- amount came from a Summary subtotal
        -- Stable identity across runs. See DESIGN NOTES above.
        -- Length is capped at 800 so (ReportRunId + RowPath) fits SQL Server's 1700-byte
        -- nonclustered index key limit (4 + 800*2 = 1604). QBO caps account names at 100 chars
        -- and sub-account nesting at 5 levels, so real paths land well under this.
        RowPath         NVARCHAR(800)  NOT NULL,
        ParentRowPath   NVARCHAR(800)  NULL,
        CONSTRAINT UQ_QBOReportRow_Run_Number UNIQUE (ReportRunId, RowNumber)
    );

    CREATE NONCLUSTERED INDEX IX_QBOReportRow_Run_Path
        ON dbo.QBOReportRow (ReportRunId, RowPath);
END
ELSE
    PRINT 'dbo.QBOReportRow already exists - skipped.';
GO

IF OBJECT_ID('dbo.QBOReportRowColumnValue', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.QBOReportRowColumnValue
    (
        Id           BIGINT         IDENTITY(1,1) PRIMARY KEY,
        ReportRunId  INT            NOT NULL
            REFERENCES dbo.QBOReportRun(Id) ON DELETE CASCADE,
        RowNumber    INT            NOT NULL,
        ColumnNumber INT            NOT NULL,
        Amount       DECIMAL(18, 2) NULL,   -- parsed; NULL when QBO sent an empty cell
        RawValue     NVARCHAR(255)  NULL,   -- verbatim, so nothing is lost to parsing
        CONSTRAINT UQ_QBOReportRowColumnValue_Cell UNIQUE (ReportRunId, RowNumber, ColumnNumber)
    );
END
ELSE
    PRINT 'dbo.QBOReportRowColumnValue already exists - skipped.';
GO
