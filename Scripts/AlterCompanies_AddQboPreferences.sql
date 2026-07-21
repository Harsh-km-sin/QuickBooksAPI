-- Adds QBO company metadata required by Reports sync.
-- NOTE: Run this script manually against the application database.
--
-- AccountingBasis      - ReportPrefs.ReportBasis from GET /v3/company/{realmId}/preferences.
--                        "Cash" or "Accrual" ("Both" is normalized to Accrual in code).
--                        Every synced report is pulled under this basis. If it ever changes,
--                        all stored reports are inconsistent and must be fully re-synced.
-- CompanyStartDate     - CompanyInfo.CompanyStartDate. Lower bound for full-history report backfill.
--                        NULL means "discover it by walking back until a window returns no data".
-- FiscalYearStartMonth - CompanyInfo.FiscalYearStartMonth (QBO sends a month NAME; parsed to 1-12).
--                        Pins the Balance Sheet start_date so the retained-earnings / net-income
--                        split is stable and independent of our chunk boundaries.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies') AND name = 'AccountingBasis')
BEGIN
    ALTER TABLE dbo.Companies ADD AccountingBasis NVARCHAR(20) NULL;
    PRINT 'Added dbo.Companies.AccountingBasis';
END
ELSE
    PRINT 'dbo.Companies.AccountingBasis already exists - skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies') AND name = 'CompanyStartDate')
BEGIN
    ALTER TABLE dbo.Companies ADD CompanyStartDate DATE NULL;
    PRINT 'Added dbo.Companies.CompanyStartDate';
END
ELSE
    PRINT 'dbo.Companies.CompanyStartDate already exists - skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies') AND name = 'FiscalYearStartMonth')
BEGIN
    ALTER TABLE dbo.Companies ADD FiscalYearStartMonth INT NULL;
    PRINT 'Added dbo.Companies.FiscalYearStartMonth';
END
ELSE
    PRINT 'dbo.Companies.FiscalYearStartMonth already exists - skipped.';
GO
