-- Close issues and data quality findings for Phase 3
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.close_issues', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.close_issues
    (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        UserId     INT            NOT NULL,
        RealmId    NVARCHAR(50)   NOT NULL,
        IssueType  NVARCHAR(100)  NOT NULL,
        Severity   NVARCHAR(50)   NOT NULL DEFAULT 'Medium',
        Details    NVARCHAR(MAX)  NULL,
        DetectedAt DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        ResolvedAt DATETIME2(7)   NULL
    );
    CREATE NONCLUSTERED INDEX IX_close_issues_UserId_RealmId_ResolvedAt ON dbo.close_issues (UserId, RealmId, ResolvedAt);
END;
