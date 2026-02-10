-- Deploy order: 1) TVP first, 2) Stored procedure second

-- 1. Table-Valued Type for Product upsert
IF TYPE_ID('dbo.ProductUpsertType') IS NULL
BEGIN
    CREATE TYPE dbo.ProductUpsertType AS TABLE (
        QBOId               NVARCHAR(50)   NOT NULL,
        Name                NVARCHAR(255)  NOT NULL,
        Description         NVARCHAR(MAX)  NULL,
        Active              BIT            NOT NULL,
        FullyQualifiedName   NVARCHAR(255)  NOT NULL,
        Taxable             BIT            NOT NULL,
        UnitPrice           DECIMAL(18,2)  NOT NULL,
        Type                NVARCHAR(50)   NOT NULL,
        IncomeAccountRefValue NVARCHAR(50)  NULL,
        IncomeAccountRefName  NVARCHAR(255) NULL,
        PurchaseCost        DECIMAL(18,2)  NOT NULL,
        TrackQtyOnHand      BIT            NOT NULL,
        QtyOnHand           DECIMAL(18,4)  NULL,
        Domain              NVARCHAR(50)   NULL,
        Sparse              BIT            NOT NULL,
        SyncToken           NVARCHAR(50)   NOT NULL,
        CreateTime          DATETIME2      NOT NULL,
        LastUpdatedTime     DATETIME2      NOT NULL,
        UserId              INT            NOT NULL,
        RealmId             NVARCHAR(50)   NOT NULL
    );
END
GO

-- 2. Stored procedure for Product upsert
CREATE OR ALTER PROCEDURE dbo.UpsertProduct
    @Products dbo.ProductUpsertType READONLY
AS
BEGIN
    SET NOCOUNT OFF;  -- Required for Dapper ExecuteAsync to return row count

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
            PurchaseCost = source.PurchaseCost,
            TrackQtyOnHand = source.TrackQtyOnHand,
            QtyOnHand = source.QtyOnHand,
            Domain = source.Domain,
            Sparse = source.Sparse,
            SyncToken = source.SyncToken,
            CreateTime = source.CreateTime,
            LastUpdatedTime = source.LastUpdatedTime
    WHEN NOT MATCHED THEN
        INSERT (
            QBOId, Name, Description, Active, FullyQualifiedName, Taxable, UnitPrice, Type,
            IncomeAccountRefValue, IncomeAccountRefName, PurchaseCost, TrackQtyOnHand, QtyOnHand,
            Domain, Sparse, SyncToken, CreateTime, LastUpdatedTime, UserId, RealmId
        )
        VALUES (
            source.QBOId, source.Name, source.Description, source.Active, source.FullyQualifiedName,
            source.Taxable, source.UnitPrice, source.Type, source.IncomeAccountRefValue,
            source.IncomeAccountRefName, source.PurchaseCost, source.TrackQtyOnHand, source.QtyOnHand,
            source.Domain, source.Sparse, source.SyncToken, source.CreateTime, source.LastUpdatedTime,
            source.UserId, source.RealmId
        );
END;
GO
