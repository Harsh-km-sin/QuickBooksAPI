-- GL Review — GL_BusinessRules table
-- User-defined Tier-3 custom detection rules. Active rules are loaded by the
-- Python worker at job start and evaluated against every GL entry.
-- NOTE: Run this script manually against the application database.

IF OBJECT_ID('dbo.GL_BusinessRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GL_BusinessRules
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT           NOT NULL,
        RuleName    NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,

        -- Rule condition stored as JSON. Two supported schemas:
        --
        -- Type 1 — Field condition:
        -- { "type": "field", "field": "amount", "operator": "gt", "value": 50000 }
        -- Operators: gt | gte | lt | lte | eq | contains | not_contains | is_null
        -- Fields:    amount | account_name | account_type | entity_name |
        --            description | source_type | created_by | posting_type
        --
        -- Type 2 — Account threshold:
        -- { "type": "account_threshold", "account": "Petty Cash",
        --   "direction": "debit", "amount": 500 }
        -- Flags: entry.account_name == account AND entry[direction] > amount
        [Condition]  NVARCHAR(MAX) NOT NULL,

        -- Maps to Tier-3 signal score: critical=1.0 high=0.75 medium=0.50 low=0.30
        Severity    NVARCHAR(20) NOT NULL DEFAULT 'medium',  -- critical|high|medium|low

        IsActive    BIT       NOT NULL DEFAULT 1,
        CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE NONCLUSTERED INDEX IX_GL_BusinessRules_UserId ON dbo.GL_BusinessRules (UserId);
    CREATE NONCLUSTERED INDEX IX_GL_BusinessRules_UserId_IsActive ON dbo.GL_BusinessRules (UserId, IsActive);

    PRINT 'GL_BusinessRules table created.';
END
ELSE
    PRINT 'GL_BusinessRules table already exists — skipped.';
