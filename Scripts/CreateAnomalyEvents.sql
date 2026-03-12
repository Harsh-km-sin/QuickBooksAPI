-- Anomaly events table for Phase 2 anomaly detection
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.anomaly_events', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.anomaly_events
    (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        UserId     INT            NOT NULL,
        RealmId    NVARCHAR(50)   NOT NULL,
        [Type]     NVARCHAR(100)  NOT NULL,
        Severity   NVARCHAR(50)   NOT NULL,
        Details    NVARCHAR(MAX) NULL,
        DetectedAt DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE NONCLUSTERED INDEX IX_anomaly_events_UserId_RealmId_DetectedAt
        ON dbo.anomaly_events (UserId, RealmId, DetectedAt DESC);
END;
