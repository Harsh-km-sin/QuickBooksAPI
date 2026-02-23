-- Add Expense, Asset account ref columns and InvStartDate to Products table

ALTER TABLE Products
    ADD ExpenseAccountRefValue NVARCHAR(50) NULL,
        ExpenseAccountRefName NVARCHAR(255) NULL,
        AssetAccountRefValue NVARCHAR(50) NULL,
        AssetAccountRefName NVARCHAR(255) NULL,
        InvStartDate NVARCHAR(20) NULL;
GO

-- Recreate the TVP to include new columns
-- Must drop dependent stored procedure first
DROP PROCEDURE IF EXISTS dbo.UpsertProduct;
GO

DROP TYPE IF EXISTS dbo.ProductUpsertType;
GO

CREATE TYPE dbo.ProductUpsertType AS TABLE (
    QBOId                   NVARCHAR(50)   NOT NULL,
    Name                    NVARCHAR(255)  NOT NULL,
    Description             NVARCHAR(MAX)  NULL,
    Active                  BIT            NOT NULL,
    FullyQualifiedName      NVARCHAR(255)  NOT NULL,
    Taxable                 BIT            NOT NULL,
    UnitPrice               DECIMAL(18,2)  NOT NULL,
    Type                    NVARCHAR(50)   NOT NULL,
    IncomeAccountRefValue   NVARCHAR(50)   NULL,
    IncomeAccountRefName    NVARCHAR(255)  NULL,
    ExpenseAccountRefValue  NVARCHAR(50)   NULL,
    ExpenseAccountRefName   NVARCHAR(255)  NULL,
    AssetAccountRefValue    NVARCHAR(50)   NULL,
    AssetAccountRefName     NVARCHAR(255)  NULL,
    PurchaseCost            DECIMAL(18,2)  NOT NULL,
    TrackQtyOnHand          BIT            NOT NULL,
    QtyOnHand               DECIMAL(18,4)  NULL,
    InvStartDate            NVARCHAR(20)   NULL,
    Domain                  NVARCHAR(50)   NULL,
    Sparse                  BIT            NOT NULL,
    SyncToken               NVARCHAR(50)   NOT NULL,
    CreateTime              DATETIME2      NOT NULL,
    LastUpdatedTime         DATETIME2      NOT NULL,
    UserId                  INT            NOT NULL,
    RealmId                 NVARCHAR(50)   NOT NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.UpsertProduct
    @Products dbo.ProductUpsertType READONLY
AS
BEGIN
    SET NOCOUNT OFF;

    MERGE dbo.Products AS target
    USING @Products AS source
    ON target.QBOId = source.QBOId 
       AND target.UserId = source.UserId 
       AND target.RealmId = source.RealmId
    WHEN MATCHED THEN
        UPDATE SET
            Name = source.Name,
            Description = source.Description,
            Active = source.Active,
            FullyQualifiedName = source.FullyQualifiedName,
            Taxable = source.Taxable,
            UnitPrice = source.UnitPrice,
            Type = source.Type,
            IncomeAccountRefValue = source.IncomeAccountRefValue,
            IncomeAccountRefName = source.IncomeAccountRefName,
            ExpenseAccountRefValue = source.ExpenseAccountRefValue,
            ExpenseAccountRefName = source.ExpenseAccountRefName,
            AssetAccountRefValue = source.AssetAccountRefValue,
            AssetAccountRefName = source.AssetAccountRefName,
            PurchaseCost = source.PurchaseCost,
            TrackQtyOnHand = source.TrackQtyOnHand,
            QtyOnHand = source.QtyOnHand,
            InvStartDate = source.InvStartDate,
            Domain = source.Domain,
            Sparse = source.Sparse,
            SyncToken = source.SyncToken,
            CreateTime = source.CreateTime,
            LastUpdatedTime = source.LastUpdatedTime
    WHEN NOT MATCHED THEN
        INSERT (
            QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
            IncomeAccountRefValue, IncomeAccountRefName, ExpenseAccountRefValue, ExpenseAccountRefName,
            AssetAccountRefValue, AssetAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
            InvStartDate, Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId
        )
        VALUES (
            source.QBOId, source.Name, source.Description, source.Active, source.FullyQualifiedName,
            source.Taxable, source.UnitPrice, source.Type, source.IncomeAccountRefValue,
            source.IncomeAccountRefName, source.ExpenseAccountRefValue, source.ExpenseAccountRefName,
            source.AssetAccountRefValue, source.AssetAccountRefName, source.PurchaseCost,
            source.TrackQtyOnHand, source.QtyOnHand, source.InvStartDate, source.Domain, source.Sparse,
            source.SyncToken, source.CreateTime, source.LastUpdatedTime, source.UserId, source.RealmId
        );
END;
GO
