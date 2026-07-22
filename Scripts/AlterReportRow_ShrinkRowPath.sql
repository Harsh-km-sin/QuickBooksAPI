-- Shrinks dbo.QBOReportRow.RowPath / ParentRowPath from NVARCHAR(1000) to NVARCHAR(800).
-- NOTE: Run this script manually against the application database.
--
-- WHY: the nonclustered index IX_QBOReportRow_Run_Path keys on (ReportRunId, RowPath).
-- At NVARCHAR(1000) that is 4 + 2000 = 2004 bytes, over SQL Server's 1700-byte index key limit,
-- which SQL Server reports only as a warning at CREATE time:
--
--     Warning! The maximum key length for a nonclustered index is 1700 bytes.
--     The index 'IX_QBOReportRow_Run_Path' has maximum length of 2004 bytes.
--
-- The index is created, but any row whose RowPath exceeds 848 characters would fail on INSERT
-- later, at runtime. At NVARCHAR(800) the key is 4 + 1600 = 1604 bytes and the failure mode is
-- gone. Real QBO paths are far shorter: account names cap at 100 characters and sub-account
-- nesting caps at 5 levels.
--
-- Only needed on databases where CreateReportTables.sql was applied BEFORE this fix. On a fresh
-- database CreateReportTables.sql already creates the columns at NVARCHAR(800) and this script
-- is a no-op.
--
-- The column must be altered while it is not part of an index key, so the index is dropped and
-- recreated around the ALTERs.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.QBOReportRow')
      AND name = 'RowPath'
      AND max_length > 1600   -- max_length is in bytes; NVARCHAR(800) = 1600
)
BEGIN
    PRINT 'dbo.QBOReportRow.RowPath is already NVARCHAR(800) or smaller - nothing to do.';
END
ELSE
BEGIN
    PRINT 'Shrinking dbo.QBOReportRow.RowPath / ParentRowPath to NVARCHAR(800)...';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QBOReportRow_Run_Path' AND object_id = OBJECT_ID('dbo.QBOReportRow'))
    BEGIN
        DROP INDEX IX_QBOReportRow_Run_Path ON dbo.QBOReportRow;
        PRINT '  Dropped IX_QBOReportRow_Run_Path.';
    END

    ALTER TABLE dbo.QBOReportRow ALTER COLUMN RowPath NVARCHAR(800) NOT NULL;
    ALTER TABLE dbo.QBOReportRow ALTER COLUMN ParentRowPath NVARCHAR(800) NULL;
    PRINT '  Columns altered.';

    CREATE NONCLUSTERED INDEX IX_QBOReportRow_Run_Path
        ON dbo.QBOReportRow (ReportRunId, RowPath);
    PRINT '  Recreated IX_QBOReportRow_Run_Path (key is now 1604 bytes).';
END
GO
