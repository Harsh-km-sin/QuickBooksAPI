-- GL Review — GL_Settings table
-- Per-user configuration: tier weights, detection thresholds, LLM provider,
-- per-detector enable flags, alert destinations, fiscal year, scheduled runs.
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.GL_Settings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GL_Settings
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NOT NULL,

        -- ── Risk tier thresholds (0-100 integer scale) ────────────────────────
        ThresholdCritical   INT NOT NULL DEFAULT 65,
        ThresholdHigh       INT NOT NULL DEFAULT 40,
        ThresholdMedium     INT NOT NULL DEFAULT 20,
        ThresholdLow        INT NOT NULL DEFAULT 8,

        -- ── Tier weights (worker auto-normalizes to 1.0 if sum != 1.0) ────────
        WeightTier1Statistical  DECIMAL(4, 3) NOT NULL DEFAULT 0.250,
        WeightTier2Ml           DECIMAL(4, 3) NOT NULL DEFAULT 0.350,
        WeightTier3Rules        DECIMAL(4, 3) NOT NULL DEFAULT 0.250,
        WeightTier4Llm          DECIMAL(4, 3) NOT NULL DEFAULT 0.150,

        -- ── LLM configuration ────────────────────────────────────────────────
        LlmProvider     NVARCHAR(20)  NOT NULL DEFAULT 'anthropic',  -- anthropic|openai|gemini|disabled
        LlmModel        NVARCHAR(100) NOT NULL DEFAULT 'claude-sonnet-4-6',
        ApiKeyAnthropic NVARCHAR(500) NULL,
        ApiKeyOpenai    NVARCHAR(500) NULL,
        ApiKeyGoogle    NVARCHAR(500) NULL,

        -- ── Tier-1 detector toggles ───────────────────────────────────────────
        EnableBenfordLaw        BIT NOT NULL DEFAULT 1,
        EnableRoundNumber       BIT NOT NULL DEFAULT 1,
        EnableThresholdBreach   BIT NOT NULL DEFAULT 1,
        EnableBackdating        BIT NOT NULL DEFAULT 1,
        EnablePeriodEndCluster  BIT NOT NULL DEFAULT 1,
        EnableFraudPatterns     BIT NOT NULL DEFAULT 1,
        EnableNearDuplicates    BIT NOT NULL DEFAULT 1,

        -- ── Tier-2 ML detector toggles ────────────────────────────────────────
        EnableIsolationForest   BIT NOT NULL DEFAULT 1,
        EnableDbscan            BIT NOT NULL DEFAULT 1,
        EnableAssociationRule   BIT NOT NULL DEFAULT 1,
        EnableCopod             BIT NOT NULL DEFAULT 1,
        EnableEcod              BIT NOT NULL DEFAULT 1,
        EnableBehaviorProfiling BIT NOT NULL DEFAULT 1,  -- controls account_behavior + entity_behavior

        -- ── Notification destinations ────────────────────────────────────────
        EmailAlerts  BIT          NOT NULL DEFAULT 0,
        AlertEmail   NVARCHAR(500) NULL,      -- single address or comma-separated
        SlackWebhook NVARCHAR(500) NULL,      -- Slack incoming webhook URL

        -- ── Fiscal year ───────────────────────────────────────────────────────
        FiscalYearStartMonth INT NOT NULL DEFAULT 1,   -- 1=Jan, 4=Apr, 7=Jul, etc.

        -- ── Related party list ────────────────────────────────────────────────
        -- JSON: [{"name": "Acme Holdings", "relationship": "parent_company"}, ...]
        RelatedPartyList NVARCHAR(MAX) NULL,

        -- ── Scheduled auto-runs ───────────────────────────────────────────────
        ScheduledEnabled   BIT          NOT NULL DEFAULT 0,
        ScheduledFrequency NVARCHAR(20) NULL,   -- daily|weekly|monthly
        ScheduledDayOfWeek INT          NULL,   -- 0=Sunday ... 6=Saturday (weekly only)
        ScheduledHour      INT          NULL,   -- 0-23 UTC hour to trigger

        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- One settings row per user
    CREATE UNIQUE NONCLUSTERED INDEX UQ_GL_Settings_UserId ON dbo.GL_Settings (UserId);

    PRINT 'GL_Settings table created.';
END
ELSE
    PRINT 'GL_Settings table already exists — skipped.';
