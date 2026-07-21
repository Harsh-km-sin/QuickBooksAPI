-- GL Review Phase 1 — Extend GL_Runs with new columns
-- NOTE: Run this script manually against the application database.
-- Safe to re-run: each ALTER checks for column existence before adding.

-- ProgressPercentage: live worker progress 0-100 updated per phase
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'ProgressPercentage')
    ALTER TABLE dbo.GL_Runs ADD ProgressPercentage INT NOT NULL DEFAULT 0;

-- Per-tier counts (MediumCount, LowCount, NormalCount fill the gap from CriticalCount/HighCount)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'MediumCount')
    ALTER TABLE dbo.GL_Runs ADD MediumCount INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'LowCount')
    ALTER TABLE dbo.GL_Runs ADD LowCount INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'NormalCount')
    ALTER TABLE dbo.GL_Runs ADD NormalCount INT NULL;

-- MaterialExposure: sum of amounts for Critical+High entries
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'MaterialExposure')
    ALTER TABLE dbo.GL_Runs ADD MaterialExposure DECIMAL(18, 2) NULL;

-- AiExecutiveSummary: LLM-generated plain-text run overview (set after Tier-4 completes)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'AiExecutiveSummary')
    ALTER TABLE dbo.GL_Runs ADD AiExecutiveSummary NVARCHAR(MAX) NULL;

-- SourceFormat: detected ERP format — 'quickbooks' or 'generic'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'SourceFormat')
    ALTER TABLE dbo.GL_Runs ADD SourceFormat NVARCHAR(20) NULL;

-- FileSizeBytes / FileType: metadata captured at upload time
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'FileSizeBytes')
    ALTER TABLE dbo.GL_Runs ADD FileSizeBytes BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'FileType')
    ALTER TABLE dbo.GL_Runs ADD FileType NVARCHAR(10) NULL;

-- StartedAt: when the worker picked up the job (distinct from CreatedAt which is upload time)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Runs') AND name = 'StartedAt')
    ALTER TABLE dbo.GL_Runs ADD StartedAt DATETIME2 NULL;

PRINT 'AlterGlRuns_Phase1: complete.';
