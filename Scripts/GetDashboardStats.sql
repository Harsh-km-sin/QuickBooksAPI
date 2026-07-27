-- Dashboard stats: single round-trip, single result row.
-- Calls the existing GetProfitAndLossTree / GetBalanceSheetTree SPs internally
-- to reuse their tested aggregation logic, then extracts only the top-level summary amounts.
-- Entity counts come from five simple COUNT(*) queries.
--
-- NOTE: Run this script manually against the application database,
--       AFTER CreateReportViews.sql (the tree SPs must already exist).

CREATE OR ALTER PROCEDURE dbo.GetDashboardStats
    @UserId  INT,
    @RealmId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- ---------------------------------------------------------------
    -- 1. Capture P&L tree (YTD, Accrual)
    -- ---------------------------------------------------------------
    DECLARE @PnlStart DATE = DATEFROMPARTS(YEAR(GETUTCDATE()), 1, 1);
    DECLARE @PnlEnd   DATE = CAST(GETUTCDATE() AS DATE);

    CREATE TABLE #PnL (
        RowNumber     INT,
        RowPath       NVARCHAR(800),
        ParentRowPath NVARCHAR(800),
        Depth         INT,
        RowType       NVARCHAR(20),
        GroupName     NVARCHAR(100),
        Label         NVARCHAR(500),
        AccountQboId  NVARCHAR(50),
        IsSummaryRow  INT,
        Amount        DECIMAL(18,2)
    );

    INSERT INTO #PnL
    EXEC dbo.GetProfitAndLossTree
        @UserId           = @UserId,
        @RealmId          = @RealmId,
        @RangeStart       = @PnlStart,
        @RangeEnd         = @PnlEnd,
        @AccountingMethod = N'Accrual';

    -- ---------------------------------------------------------------
    -- 2. Capture Balance Sheet tree (as of today, Accrual)
    -- ---------------------------------------------------------------
    CREATE TABLE #BS (
        RowNumber     INT,
        RowPath       NVARCHAR(800),
        ParentRowPath NVARCHAR(800),
        Depth         INT,
        RowType       NVARCHAR(20),
        GroupName     NVARCHAR(100),
        Label         NVARCHAR(500),
        AccountQboId  NVARCHAR(50),
        IsSummaryRow  INT,
        Amount        DECIMAL(18,2)
    );

    INSERT INTO #BS
    EXEC dbo.GetBalanceSheetTree
        @UserId           = @UserId,
        @RealmId          = @RealmId,
        @RangeStart       = @PnlEnd,
        @RangeEnd         = @PnlEnd,
        @AccountingMethod = N'Accrual';

    -- ---------------------------------------------------------------
    -- 3. Extract summary figures and entity counts
    -- ---------------------------------------------------------------
    SELECT
        -- P&L summaries (YTD)
        (SELECT TOP 1 Amount FROM #PnL WHERE GroupName = 'Income' AND IsSummaryRow = 1)
            AS TotalIncome,
        (SELECT TOP 1 Amount FROM #PnL WHERE GroupName = 'Expenses' AND IsSummaryRow = 1)
            AS TotalExpenses,
        (SELECT TOP 1 Amount FROM #PnL WHERE GroupName = 'NetIncome' AND IsSummaryRow = 1)
            AS NetIncome,
        @PnlStart AS PnlRangeStart,
        @PnlEnd   AS PnlRangeEnd,
        N'Accrual' AS PnlAccountingMethod,

        -- Balance Sheet summaries (as-of)
        (SELECT TOP 1 Amount FROM #BS WHERE GroupName = 'TotalAssets' AND IsSummaryRow = 1)
            AS TotalAssets,
        (SELECT TOP 1 Amount FROM #BS WHERE GroupName = 'Liabilities' AND IsSummaryRow = 1)
            AS TotalLiabilities,
        (SELECT TOP 1 Amount FROM #BS WHERE GroupName = 'Equity' AND IsSummaryRow = 1)
            AS TotalEquity,
        @PnlEnd    AS BsAsOfDate,
        N'Accrual' AS BsAccountingMethod,

        -- Entity counts
        (SELECT COUNT(*) FROM dbo.Customer         WHERE UserId = @UserId AND RealmId = @RealmId AND Active = 1)
            AS CustomersCount,
        (SELECT COUNT(*) FROM dbo.Vendor            WHERE UserId = @UserId AND RealmId = @RealmId AND DeletedAt IS NULL)
            AS VendorsCount,
        (SELECT COUNT(*) FROM dbo.Products          WHERE UserId = @UserId AND RealmId = @RealmId AND Active = 1)
            AS ProductsCount,
        (SELECT COUNT(*) FROM dbo.QBOInvoiceHeader  WHERE RealmId = @RealmId)
            AS InvoicesCount,
        (SELECT COUNT(*) FROM dbo.QBOBillHeader     WHERE RealmId = @RealmId AND (IsDeleted = 0 OR IsDeleted IS NULL))
            AS BillsCount;

    DROP TABLE #PnL;
    DROP TABLE #BS;
END;
GO
