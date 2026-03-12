-- Financial warehouse schema for analytics
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.DimCustomer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DimCustomer
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserId          INT            NOT NULL,
        RealmId         NVARCHAR(50)   NOT NULL,
        CustomerQboId   NVARCHAR(50)   NOT NULL,
        CustomerName    NVARCHAR(255)  NOT NULL
    );
END;

IF OBJECT_ID('dbo.DimVendor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DimVendor
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        UserId        INT            NOT NULL,
        RealmId       NVARCHAR(50)   NOT NULL,
        VendorQboId   NVARCHAR(50)   NOT NULL,
        VendorName    NVARCHAR(255)  NOT NULL
    );
END;

IF OBJECT_ID('dbo.DimAccount', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DimAccount
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserId          INT            NOT NULL,
        RealmId         NVARCHAR(50)   NOT NULL,
        AccountQboId    NVARCHAR(50)   NOT NULL,
        AccountName     NVARCHAR(255)  NOT NULL,
        AccountType     NVARCHAR(100)  NULL,
        Classification  NVARCHAR(100)  NULL
    );
END;

IF OBJECT_ID('dbo.FactRevenue', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactRevenue
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        UserId        INT            NOT NULL,
        RealmId       NVARCHAR(50)   NOT NULL,
        [Date]        DATE           NOT NULL,
        CustomerDimId INT            NULL,
        AccountDimId  INT            NULL,
        InvoiceQboId  NVARCHAR(50)   NOT NULL,
        Amount        DECIMAL(18, 2) NOT NULL,
        TaxAmount     DECIMAL(18, 2) NOT NULL,
        NetAmount     DECIMAL(18, 2) NOT NULL
    );
END;

IF OBJECT_ID('dbo.FactExpenses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactExpenses
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        UserId        INT            NOT NULL,
        RealmId       NVARCHAR(50)   NOT NULL,
        [Date]        DATE           NOT NULL,
        VendorDimId   INT            NULL,
        AccountDimId  INT            NULL,
        BillQboId     NVARCHAR(50)   NOT NULL,
        Amount        DECIMAL(18, 2) NOT NULL,
        TaxAmount     DECIMAL(18, 2) NOT NULL,
        NetAmount     DECIMAL(18, 2) NOT NULL
    );
END;

IF OBJECT_ID('dbo.FactVendorSpend', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactVendorSpend
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        UserId        INT            NOT NULL,
        RealmId       NVARCHAR(50)   NOT NULL,
        VendorDimId   INT            NOT NULL,
        PeriodStart   DATE           NOT NULL,
        PeriodEnd     DATE           NOT NULL,
        TotalSpend    DECIMAL(18, 2) NOT NULL,
        BillCount     INT            NOT NULL,
        LastBillDate  DATE           NULL
    );
END;

IF OBJECT_ID('dbo.FactCustomerProfitability', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactCustomerProfitability
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        UserId         INT            NOT NULL,
        RealmId        NVARCHAR(50)   NOT NULL,
        CustomerDimId  INT            NOT NULL,
        PeriodStart    DATE           NOT NULL,
        PeriodEnd      DATE           NOT NULL,
        Revenue        DECIMAL(18, 2) NOT NULL,
        CostOfGoods    DECIMAL(18, 2) NOT NULL
    );
END;

