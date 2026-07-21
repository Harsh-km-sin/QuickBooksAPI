-- GL Review — GL_Anomalies table
-- One row per (entry × fired detector). Stores per-detector evidence for
-- the anomaly detail drawer in the Transaction Explorer UI.
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.GL_Anomalies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GL_Anomalies
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,

        -- Foreign key to the transaction row (cascades on run delete via GL_Transactions)
        EntryId       INT NOT NULL REFERENCES dbo.GL_Transactions(Id) ON DELETE CASCADE,

        -- Denormalized for fast aggregate queries without joining GL_Transactions
        RunId         INT NOT NULL,
        UserId        INT NOT NULL,

        -- Detector identity
        AnomalyType   NVARCHAR(100) NOT NULL,  -- e.g. "z_score_outlier", "benford_law", "isolation_forest"

        -- Per-detector signal strength 0.0-1.0 (before tier weighting)
        DetectorScore DECIMAL(5, 4) NOT NULL,

        -- Human-readable reasons from this detector (JSON string array)
        -- e.g. ["Amount $45,000 is 3.8σ above account peer mean $12,400 (n=42 rows)"]
        RiskReasons   NVARCHAR(MAX) NULL,

        -- Detector-specific evidence payload (JSON object)
        -- z_score_outlier:    {"zScore": 3.8, "accountMean": 12400, "accountStdDev": 8500}
        -- benford_law:        {"firstDigit": 7, "expected": 0.058, "actual": 0.14}
        -- isolation_forest:   {"anomalyScore": 0.72, "topDrivers": ["log_amount", "is_month_end"]}
        -- suspicious_keyword: {"keyword": "adjustment", "matchedIn": "description"}
        Metadata      NVARCHAR(MAX) NULL
    );

    CREATE NONCLUSTERED INDEX IX_GL_Anomalies_EntryId ON dbo.GL_Anomalies (EntryId);
    CREATE NONCLUSTERED INDEX IX_GL_Anomalies_RunId ON dbo.GL_Anomalies (RunId);
    CREATE NONCLUSTERED INDEX IX_GL_Anomalies_RunId_AnomalyType ON dbo.GL_Anomalies (RunId, AnomalyType);

    PRINT 'GL_Anomalies table created.';
END
ELSE
    PRINT 'GL_Anomalies table already exists — skipped.';
