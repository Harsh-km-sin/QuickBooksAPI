-- Upsert for one stored QBO report pull (header + columns + rows + cell values).
-- NOTE: Run this script manually against the application database, AFTER CreateReportTables.sql.
--
-- Deploy order matters: drop the procedure before dropping/recreating the types, or SQL Server
-- refuses to drop a type still referenced by a procedure signature.
--
-- The caller (ReportRepository) owns the transaction, matching the Bill/Invoice sync pattern,
-- so this procedure deliberately does not BEGIN/COMMIT its own.

IF OBJECT_ID('dbo.UpsertReportRun', 'P') IS NOT NULL
    DROP PROCEDURE dbo.UpsertReportRun;
GO

IF TYPE_ID('dbo.ReportRunUpsertType') IS NOT NULL           DROP TYPE dbo.ReportRunUpsertType;
GO
IF TYPE_ID('dbo.ReportColumnUpsertType') IS NOT NULL        DROP TYPE dbo.ReportColumnUpsertType;
GO
IF TYPE_ID('dbo.ReportRowUpsertType') IS NOT NULL           DROP TYPE dbo.ReportRowUpsertType;
GO
IF TYPE_ID('dbo.ReportRowColumnValueUpsertType') IS NOT NULL DROP TYPE dbo.ReportRowColumnValueUpsertType;
GO

-- 1. Table-valued types

CREATE TYPE dbo.ReportRunUpsertType AS TABLE (
    UserId           INT             NOT NULL,
    RealmId          NVARCHAR(50)    NOT NULL,
    ReportType       NVARCHAR(50)    NOT NULL,
    Granularity      NVARCHAR(20)    NOT NULL,
    AccountingMethod NVARCHAR(20)    NOT NULL,
    ValueSemantics   NVARCHAR(10)    NOT NULL,
    PeriodStart      DATE            NOT NULL,
    PeriodEnd        DATE            NOT NULL,
    Currency         NVARCHAR(10)    NULL,
    NoReportData     BIT             NOT NULL,
    GeneratedAtUtc   DATETIMEOFFSET  NULL,
    RawJson          NVARCHAR(MAX)   NULL
);
GO

CREATE TYPE dbo.ReportColumnUpsertType AS TABLE (
    ColumnNumber   INT           NOT NULL,
    ColKey         NVARCHAR(100) NULL,
    ColTitle       NVARCHAR(255) NULL,
    ColType        NVARCHAR(50)  NULL,
    ColPeriodStart DATE          NULL,
    ColPeriodEnd   DATE          NULL
);
GO

CREATE TYPE dbo.ReportRowUpsertType AS TABLE (
    RowNumber       INT            NOT NULL,
    ParentRowNumber INT            NULL,
    Depth           INT            NOT NULL,
    RowType         NVARCHAR(20)   NOT NULL,
    GroupName       NVARCHAR(100)  NULL,
    Label           NVARCHAR(500)  NULL,
    AccountQboId    NVARCHAR(50)   NULL,
    IsSummaryRow    BIT            NOT NULL,
    RowPath         NVARCHAR(800)  NOT NULL,
    ParentRowPath   NVARCHAR(800)  NULL
);
GO

CREATE TYPE dbo.ReportRowColumnValueUpsertType AS TABLE (
    RowNumber    INT            NOT NULL,
    ColumnNumber INT            NOT NULL,
    Amount       DECIMAL(18, 2) NULL,
    RawValue     NVARCHAR(255)  NULL
);
GO

-- 2. Stored procedure

CREATE PROCEDURE dbo.UpsertReportRun
    @Run     dbo.ReportRunUpsertType             READONLY,
    @Columns dbo.ReportColumnUpsertType          READONLY,
    @Rows    dbo.ReportRowUpsertType             READONLY,
    @Values  dbo.ReportRowColumnValueUpsertType  READONLY
AS
BEGIN
    SET NOCOUNT OFF;  -- Required for Dapper ExecuteAsync to return a row count.

    DECLARE @Ids TABLE (ReportRunId INT);

    MERGE dbo.QBOReportRun AS target
    USING @Run AS source
        ON  target.UserId           = source.UserId
        AND target.RealmId          = source.RealmId
        AND target.ReportType       = source.ReportType
        AND target.Granularity      = source.Granularity
        AND target.AccountingMethod = source.AccountingMethod
        AND target.PeriodStart      = source.PeriodStart
        AND target.PeriodEnd        = source.PeriodEnd
    WHEN MATCHED THEN
        UPDATE SET
            ValueSemantics = source.ValueSemantics,
            Currency       = source.Currency,
            NoReportData   = source.NoReportData,
            GeneratedAtUtc = source.GeneratedAtUtc,
            RawJson        = source.RawJson,
            SyncedAtUtc    = SYSDATETIMEOFFSET(),
            UpdatedAtUtc   = SYSDATETIMEOFFSET()
    WHEN NOT MATCHED THEN
        INSERT (UserId, RealmId, ReportType, Granularity, AccountingMethod, ValueSemantics,
                PeriodStart, PeriodEnd, Currency, NoReportData, GeneratedAtUtc,
                SyncedAtUtc, RawJson, CreatedAtUtc, UpdatedAtUtc)
        VALUES (source.UserId, source.RealmId, source.ReportType, source.Granularity,
                source.AccountingMethod, source.ValueSemantics,
                source.PeriodStart, source.PeriodEnd, source.Currency, source.NoReportData,
                source.GeneratedAtUtc, SYSDATETIMEOFFSET(), source.RawJson,
                SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
    OUTPUT inserted.Id INTO @Ids(ReportRunId);

    DECLARE @ReportRunId INT = (SELECT TOP 1 ReportRunId FROM @Ids);

    IF @ReportRunId IS NULL
    BEGIN
        THROW 50001, 'UpsertReportRun: no report run row was produced. @Run must contain exactly one row.', 1;
    END

    -- Replace the period wholesale rather than merging children. The whole point of the
    -- overwrite-no-versioning design is that a re-sync fully supersedes the prior pull, and
    -- accounts can disappear between pulls (deleted/merged in QBO), which a MERGE would strand.
    DELETE FROM dbo.QBOReportRowColumnValue WHERE ReportRunId = @ReportRunId;
    DELETE FROM dbo.QBOReportRow            WHERE ReportRunId = @ReportRunId;
    DELETE FROM dbo.QBOReportColumn         WHERE ReportRunId = @ReportRunId;

    INSERT INTO dbo.QBOReportColumn
        (ReportRunId, ColumnNumber, ColKey, ColTitle, ColType, ColPeriodStart, ColPeriodEnd)
    SELECT @ReportRunId, ColumnNumber, ColKey, ColTitle, ColType, ColPeriodStart, ColPeriodEnd
    FROM @Columns;

    INSERT INTO dbo.QBOReportRow
        (ReportRunId, RowNumber, ParentRowNumber, Depth, RowType, GroupName, Label,
         AccountQboId, IsSummaryRow, RowPath, ParentRowPath)
    SELECT @ReportRunId, RowNumber, ParentRowNumber, Depth, RowType, GroupName, Label,
           AccountQboId, IsSummaryRow, RowPath, ParentRowPath
    FROM @Rows;

    INSERT INTO dbo.QBOReportRowColumnValue
        (ReportRunId, RowNumber, ColumnNumber, Amount, RawValue)
    SELECT @ReportRunId, RowNumber, ColumnNumber, Amount, RawValue
    FROM @Values;

    SELECT @ReportRunId AS ReportRunId;
END;
GO
