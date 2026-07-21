-- GL Review — GL_Feedback table
-- Auditor verdicts per transaction entry. Uses upsert model (MERGE on EntryId):
-- one row per entry, updated in place when auditor changes verdict.
-- ReviewedAt captures the most recent decision timestamp.
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.GL_Feedback', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GL_Feedback
    (
        Id               INT IDENTITY(1,1) PRIMARY KEY,

        -- One feedback record per transaction entry (upsert enforced via MERGE in repo)
        EntryId          INT NOT NULL REFERENCES dbo.GL_Transactions(Id) ON DELETE CASCADE,

        RunId            INT NOT NULL,
        UserId           INT NOT NULL,  -- last auditor to update this record

        -- Verdict: did the auditor confirm or dismiss the flag?
        -- pending | confirmed_true_positive | dismissed_false_positive
        [Status]         NVARCHAR(50) NOT NULL DEFAULT 'pending',

        -- Resolution tracking
        -- unresolved | resolved | escalated
        ResolutionStatus NVARCHAR(30) NOT NULL DEFAULT 'unresolved',

        -- Free-form auditor fields
        AuditDecision    NVARCHAR(MAX) NULL,  -- formal audit decision text
        Comments         NVARCHAR(MAX) NULL,  -- auditor notes
        RequiredEvidence NVARCHAR(MAX) NULL,  -- evidence requested / obtained
        ResolutionNotes  NVARCHAR(MAX) NULL,  -- how the issue was resolved

        -- Who reviewed it
        ReviewedBy       NVARCHAR(200) NULL,
        ReviewedAt       DATETIME2    NULL,
        AssignedTo       NVARCHAR(200) NULL
    );

    -- Unique so MERGE ON EntryId always finds at most one row
    CREATE UNIQUE NONCLUSTERED INDEX UQ_GL_Feedback_EntryId ON dbo.GL_Feedback (EntryId);
    CREATE NONCLUSTERED INDEX IX_GL_Feedback_RunId ON dbo.GL_Feedback (RunId);

    PRINT 'GL_Feedback table created.';
END
ELSE
    PRINT 'GL_Feedback table already exists — skipped.';
