-- GL Review Phase 1 — Extend GL_Transactions with new columns
-- NOTE: Run this script manually against the application database.
-- Safe to re-run: each ALTER checks for column existence before adding.

-- UserId: denormalized from GL_Runs for fast per-user queries without joins
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'UserId')
    ALTER TABLE dbo.GL_Transactions ADD UserId INT NULL;

-- RiskScore: final 0-100 integer score (replaces 0.0-1.0 float CompositeRiskScore for display)
-- CompositeRiskScore (0.0-1.0) is retained for internal calculations; RiskScore is the UI-facing value.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'RiskScore')
    ALTER TABLE dbo.GL_Transactions ADD RiskScore INT NULL;

-- AiExplanation: plain-English LLM explanation (<=500 chars, story-first, no jargon)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'AiExplanation')
    ALTER TABLE dbo.GL_Transactions ADD AiExplanation NVARCHAR(1000) NULL;

-- RiskExplanation: structured LLM JSON response
-- Schema: { suggestedAction, actionDetail, accountingRiskType, materialityAssessment,
--           contextualRiskBoost, missingReversal, proposedCorrection? }
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'RiskExplanation')
    ALTER TABLE dbo.GL_Transactions ADD RiskExplanation NVARCHAR(MAX) NULL;

-- Status: auditor workflow state — pending | reviewed | flagged
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'Status')
    ALTER TABLE dbo.GL_Transactions ADD [Status] NVARCHAR(20) NOT NULL DEFAULT 'pending';

-- Index on (RunId, RiskTier) for fast tier-filtered queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'IX_GL_Transactions_RunId_RiskTier')
    CREATE NONCLUSTERED INDEX IX_GL_Transactions_RunId_RiskTier ON dbo.GL_Transactions (RunId, RiskTier);

-- Index on (RunId, Status) for unreviewed-only queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'IX_GL_Transactions_RunId_Status')
    CREATE NONCLUSTERED INDEX IX_GL_Transactions_RunId_Status ON dbo.GL_Transactions (RunId, [Status]);

-- Index on UserId for cross-run per-user queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GL_Transactions') AND name = 'IX_GL_Transactions_UserId')
    CREATE NONCLUSTERED INDEX IX_GL_Transactions_UserId ON dbo.GL_Transactions (UserId);

PRINT 'AlterGlTransactions_Phase1: complete.';
