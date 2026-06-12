using System.Globalization;
using QuickBooksAPI.DataAccessLayer.Warehouse;

namespace QuickBooksAPI.DataAccessLayer.Sql;

/// <summary>
/// Batch SQL for warehouse fact rebuild (kept out of <c>FinancialWarehouseRepository</c> for readability).
/// </summary>
internal static class FinancialWarehouseRebuildFactsSql
{
    public static string Build(decimal customerCogsRatio = WarehouseAnalyticsPolicy.DefaultCustomerCogsRatio)
    {
        var cogsLiteral = customerCogsRatio.ToString(CultureInfo.InvariantCulture);
        return $@"
DELETE FROM FactCustomerProfitability WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactVendorSpend WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactExpenses WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM FactRevenue WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimCustomer WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimVendor WHERE UserId = @UserId AND RealmId = @RealmId;
DELETE FROM DimAccount WHERE UserId = @UserId AND RealmId = @RealmId;

-- Dimensions
INSERT INTO DimCustomer (UserId, RealmId, CustomerQboId, CustomerName)
SELECT DISTINCT @UserId, @RealmId, QboId, COALESCE(DisplayName, CompanyName, GivenName + ' ' + FamilyName)
FROM Customer
WHERE UserId = @UserId AND RealmId = @RealmId;

INSERT INTO DimVendor (UserId, RealmId, VendorQboId, VendorName)
SELECT DISTINCT @UserId, @RealmId, QboId, COALESCE(DisplayName, CompanyName)
FROM Vendor
WHERE UserId = @UserId AND RealmId = @RealmId AND (DeletedAt IS NULL);

INSERT INTO DimAccount (UserId, RealmId, AccountQboId, AccountName, AccountType, Classification)
SELECT DISTINCT @UserId, @RealmId, QboId, Name, AccountType, Classification
FROM ChartOfAccounts
WHERE UserId = @UserId AND RealmId = @RealmId;

-- Revenue facts from invoice headers (one row per invoice)
INSERT INTO FactRevenue (UserId, RealmId, Date, CustomerDimId, AccountDimId, InvoiceQboId, Amount, TaxAmount, NetAmount)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    CAST(h.TxnDate AS date) AS [Date],
    dc.Id AS CustomerDimId,
    NULL AS AccountDimId,
    h.QBOInvoiceId,
    h.TotalAmt AS Amount,
    0 AS TaxAmount,
    h.TotalAmt AS NetAmount
FROM QBOInvoiceHeader h
LEFT JOIN DimCustomer dc
    ON dc.UserId = @UserId AND dc.RealmId = @RealmId AND dc.CustomerQboId = h.CustomerRefId
WHERE h.RealmId = @RealmId;

-- Expense facts from bill headers (one row per bill)
INSERT INTO FactExpenses (UserId, RealmId, Date, VendorDimId, AccountDimId, BillQboId, Amount, TaxAmount, NetAmount)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    CAST(h.TxnDate AS date) AS [Date],
    dv.Id AS VendorDimId,
    NULL AS AccountDimId,
    h.QBOBillId,
    h.TotalAmt AS Amount,
    0 AS TaxAmount,
    h.TotalAmt AS NetAmount
FROM QBOBillHeader h
LEFT JOIN DimVendor dv
    ON dv.UserId = @UserId AND dv.RealmId = @RealmId AND dv.VendorQboId = h.VendorRefValue
WHERE h.RealmId = @RealmId AND (h.IsDeleted = 0 OR h.IsDeleted IS NULL);

-- Vendor spend by month
INSERT INTO FactVendorSpend (UserId, RealmId, VendorDimId, PeriodStart, PeriodEnd, TotalSpend, BillCount, LastBillDate)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    fe.VendorDimId,
    DATEFROMPARTS(YEAR(fe.Date), MONTH(fe.Date), 1) AS PeriodStart,
    EOMONTH(DATEFROMPARTS(YEAR(fe.Date), MONTH(fe.Date), 1)) AS PeriodEnd,
    SUM(fe.NetAmount) AS TotalSpend,
    COUNT(DISTINCT fe.BillQboId) AS BillCount,
    MAX(fe.Date) AS LastBillDate
FROM FactExpenses fe
WHERE fe.UserId = @UserId AND fe.RealmId = @RealmId AND fe.VendorDimId IS NOT NULL
GROUP BY fe.VendorDimId, YEAR(fe.Date), MONTH(fe.Date);

-- Customer profitability by month (revenue minus a simple proportional COGS proxy)
INSERT INTO FactCustomerProfitability (UserId, RealmId, CustomerDimId, PeriodStart, PeriodEnd, Revenue, CostOfGoods)
SELECT
    @UserId AS UserId,
    @RealmId AS RealmId,
    fr.CustomerDimId,
    DATEFROMPARTS(YEAR(fr.Date), MONTH(fr.Date), 1) AS PeriodStart,
    EOMONTH(DATEFROMPARTS(YEAR(fr.Date), MONTH(fr.Date), 1)) AS PeriodEnd,
    SUM(fr.NetAmount) AS Revenue,
    SUM(fr.NetAmount) * {cogsLiteral} AS CostOfGoods
FROM FactRevenue fr
WHERE fr.UserId = @UserId AND fr.RealmId = @RealmId AND fr.CustomerDimId IS NOT NULL
GROUP BY fr.CustomerDimId, YEAR(fr.Date), MONTH(fr.Date);
";
    }
}
