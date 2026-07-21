-- Reports sync database object presence check (run against the DefaultConnection database).
-- Example: sqlcmd -S localhost -d YourDb -E -i tests\scripts\verify-reports-objects.sql
--
-- Deploy order:
--   1) Scripts\AlterCompanies_AddQboPreferences.sql
--   2) Scripts\CreateReportTables.sql
--   3) Scripts\UpsertReport.sql
--   4) Scripts\CreateReportViews.sql
--
-- Every value below should be non-NULL / 1 once all four have been applied.

PRINT '--- Companies metadata columns (AlterCompanies_AddQboPreferences.sql) ---';
SELECT
    MAX(CASE WHEN name = 'AccountingBasis'      THEN 1 ELSE 0 END) AS has_AccountingBasis,
    MAX(CASE WHEN name = 'CompanyStartDate'     THEN 1 ELSE 0 END) AS has_CompanyStartDate,
    MAX(CASE WHEN name = 'FiscalYearStartMonth' THEN 1 ELSE 0 END) AS has_FiscalYearStartMonth
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.Companies');

PRINT '--- Tables (CreateReportTables.sql) ---';
SELECT OBJECT_ID(N'dbo.QBOReportRun', N'U')             AS QBOReportRun_id,
       OBJECT_ID(N'dbo.QBOReportColumn', N'U')          AS QBOReportColumn_id,
       OBJECT_ID(N'dbo.QBOReportRow', N'U')             AS QBOReportRow_id,
       OBJECT_ID(N'dbo.QBOReportRowColumnValue', N'U')  AS QBOReportRowColumnValue_id;

PRINT '--- Table-valued types + upsert procedure (UpsertReport.sql) ---';
SELECT TYPE_ID(N'dbo.ReportRunUpsertType')              AS ReportRunUpsertType_id,
       TYPE_ID(N'dbo.ReportColumnUpsertType')           AS ReportColumnUpsertType_id,
       TYPE_ID(N'dbo.ReportRowUpsertType')              AS ReportRowUpsertType_id,
       TYPE_ID(N'dbo.ReportRowColumnValueUpsertType')   AS ReportRowColumnValueUpsertType_id,
       OBJECT_ID(N'dbo.UpsertReportRun', N'P')          AS UpsertReportRun_id;

PRINT '--- Read views + procedures (CreateReportViews.sql) ---';
SELECT OBJECT_ID(N'dbo.vw_ReportLines', N'V')           AS vw_ReportLines_id,
       OBJECT_ID(N'dbo.vw_ProfitAndLossLines', N'V')    AS vw_ProfitAndLossLines_id,
       OBJECT_ID(N'dbo.vw_BalanceSheetLines', N'V')     AS vw_BalanceSheetLines_id,
       OBJECT_ID(N'dbo.GetProfitAndLossTree', N'P')     AS GetProfitAndLossTree_id,
       OBJECT_ID(N'dbo.GetBalanceSheetTree', N'P')      AS GetBalanceSheetTree_id;
