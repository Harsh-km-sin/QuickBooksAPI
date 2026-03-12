-- Forecast scenarios and results for Phase 3
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.forecast_scenarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.forecast_scenarios
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        UserId         INT            NOT NULL,
        RealmId        NVARCHAR(50)   NOT NULL,
        Name           NVARCHAR(255)  NOT NULL,
        CreatedAtUtc   DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy      NVARCHAR(255)  NULL,
        HorizonMonths  INT            NOT NULL,
        AssumptionsJson NVARCHAR(MAX) NULL,
        Status         NVARCHAR(50)   NOT NULL DEFAULT 'Pending'
    );
    CREATE NONCLUSTERED INDEX IX_forecast_scenarios_UserId_RealmId ON dbo.forecast_scenarios (UserId, RealmId);
END;

IF OBJECT_ID('dbo.forecast_results', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.forecast_results
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        ScenarioId  INT            NOT NULL,
        PeriodStart DATE           NOT NULL,
        Revenue     DECIMAL(18, 2) NOT NULL DEFAULT 0,
        Expenses    DECIMAL(18, 2) NOT NULL DEFAULT 0,
        NetIncome   DECIMAL(18, 2) NOT NULL DEFAULT 0,
        CashBalance DECIMAL(18, 2) NOT NULL DEFAULT 0,
        RunwayMonths DECIMAL(10, 2) NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        CONSTRAINT FK_forecast_results_Scenario FOREIGN KEY (ScenarioId) REFERENCES dbo.forecast_scenarios(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_forecast_results_ScenarioId_Period ON dbo.forecast_results (ScenarioId, PeriodStart);
END;
