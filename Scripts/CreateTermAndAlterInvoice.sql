-- 1. Alter dbo.QBOInvoiceHeader table to add CustomerMemo and SalesTermRefId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.QBOInvoiceHeader') AND name = 'CustomerMemo')
BEGIN
    ALTER TABLE dbo.QBOInvoiceHeader ADD CustomerMemo NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.QBOInvoiceHeader') AND name = 'SalesTermRefId')
BEGIN
    ALTER TABLE dbo.QBOInvoiceHeader ADD SalesTermRefId NVARCHAR(50) NULL;
END

-- 2. Create dbo.QBOTerm Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.QBOTerm') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.QBOTerm (
        TermId BIGINT IDENTITY(1,1) PRIMARY KEY,
        QBOTermId NVARCHAR(50) NOT NULL,
        RealmId NVARCHAR(50) NOT NULL,
        SyncToken NVARCHAR(50) NULL,
        Name NVARCHAR(100) NOT NULL,
        Active BIT NOT NULL DEFAULT 1,
        Type NVARCHAR(50) NULL,
        DiscountPercent DECIMAL(5,2) NULL,
        DiscountDays INT NULL,
        DueDays INT NULL,
        DayOfMonthDue INT NULL,
        DueNextMonthDays INT NULL,
        CreateTime DATETIMEOFFSET NULL,
        LastUpdatedTime DATETIMEOFFSET NULL,
        RawJson NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_QBOTerm_QB_Realm UNIQUE (QBOTermId, RealmId)
    );
END
