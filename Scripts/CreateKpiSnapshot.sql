-- KPI snapshot table for Phase 2 KPI engine
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.kpi_snapshot', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.kpi_snapshot
    (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        UserId       INT            NOT NULL,
        RealmId       NVARCHAR(50)   NOT NULL,
        SnapshotDate  DATE           NOT NULL,
        KpiName       NVARCHAR(100)  NOT NULL,
        KpiValue      DECIMAL(18, 6) NOT NULL,
        Period        NVARCHAR(20)   NOT NULL DEFAULT 'Monthly',
        MetadataJson  NVARCHAR(MAX)  NULL,
        CONSTRAINT UQ_kpi_snapshot_User_Realm_Date_Name_Period
            UNIQUE (UserId, RealmId, SnapshotDate, KpiName, Period)
    );

    CREATE NONCLUSTERED INDEX IX_kpi_snapshot_UserId_RealmId_SnapshotDate_KpiName
        ON dbo.kpi_snapshot (UserId, RealmId, SnapshotDate, KpiName);
END;
