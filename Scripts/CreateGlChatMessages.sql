-- GL Review — GL_ChatMessages table
-- Server-persisted multi-turn chat history per GL run.
-- Each row is one message (user or assistant). History is loaded in full
-- at the start of each chat API call to provide conversation context.
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.GL_ChatMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GL_ChatMessages
    (
        Id        INT IDENTITY(1,1) PRIMARY KEY,
        RunId     INT NOT NULL REFERENCES dbo.GL_Runs(Id) ON DELETE CASCADE,
        UserId    INT NOT NULL,

        -- user | assistant | system
        [Role]    NVARCHAR(20)  NOT NULL,

        Content   NVARCHAR(MAX) NOT NULL,

        -- Optional metadata JSON: { "model": "claude-sonnet-4-6", "inputTokens": 1200, "outputTokens": 340 }
        Metadata  NVARCHAR(MAX) NULL,

        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE NONCLUSTERED INDEX IX_GL_ChatMessages_RunId ON dbo.GL_ChatMessages (RunId);
    CREATE NONCLUSTERED INDEX IX_GL_ChatMessages_RunId_CreatedAt ON dbo.GL_ChatMessages (RunId, CreatedAt);

    PRINT 'GL_ChatMessages table created.';
END
ELSE
    PRINT 'GL_ChatMessages table already exists — skipped.';
