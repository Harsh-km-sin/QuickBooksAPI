-- Multi-company consolidation (Phase 3)
-- dim_entity: companies/realmIds that can be rolled up to a parent.
-- fact_consolidated_pnl: monthly P&L per parent entity (after FX and rollup).
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.dim_entity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.dim_entity
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        UserId              INT            NOT NULL,
        RealmId             NVARCHAR(50)   NOT NULL,
        ParentEntityId       INT            NULL,
        Name                NVARCHAR(200)  NOT NULL,
        Currency            NVARCHAR(10)   NOT NULL DEFAULT 'USD',
        IsConsolidatedNode  BIT            NOT NULL DEFAULT 0,
        CONSTRAINT FK_dim_entity_parent FOREIGN KEY (ParentEntityId) REFERENCES dbo.dim_entity(Id)
    );
    CREATE NONCLUSTERED INDEX IX_dim_entity_UserId_RealmId ON dbo.dim_entity (UserId, RealmId);
    CREATE NONCLUSTERED INDEX IX_dim_entity_ParentEntityId ON dbo.dim_entity (ParentEntityId);
END;

IF OBJECT_ID('dbo.fact_consolidated_pnl', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.fact_consolidated_pnl
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        EntityId       INT            NOT NULL,
        PeriodStart     DATE           NOT NULL,
        PeriodEnd       DATE           NOT NULL,
        Revenue        DECIMAL(18,2)  NOT NULL DEFAULT 0,
        Expenses       DECIMAL(18,2)  NOT NULL DEFAULT 0,
        NetIncome      DECIMAL(18,2)  NOT NULL DEFAULT 0,
        FxRateApplied  DECIMAL(18,6)  NULL,
        MetadataJson   NVARCHAR(MAX)  NULL,
        CONSTRAINT FK_fact_consolidated_pnl_entity FOREIGN KEY (EntityId) REFERENCES dbo.dim_entity(Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_fact_consolidated_pnl_EntityId_Period ON dbo.fact_consolidated_pnl (EntityId, PeriodStart);
END;
