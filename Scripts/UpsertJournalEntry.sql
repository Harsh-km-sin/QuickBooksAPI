-- Deploy order: 1) TVPs first, 2) Stored procedures second

-- 1. Table-Valued Type for Journal Entry Header upsert
IF TYPE_ID('dbo.JournalEntryHeaderUpsertType') IS NULL
BEGIN
    CREATE TYPE dbo.JournalEntryHeaderUpsertType AS TABLE (
        QBJournalEntryId    NVARCHAR(50)     NOT NULL,
        SyncToken          NVARCHAR(50)     NULL,
        Domain             NVARCHAR(50)     NULL,
        TxnDate            DATETIME2        NULL,
        Sparse             BIT              NULL,
        Adjustment         BIT              NULL,
        CreateTime         DATETIMEOFFSET   NULL,
        LastUpdatedTime    DATETIMEOFFSET   NULL,
        RawJson            NVARCHAR(MAX)    NULL,
        QBRealmId          NVARCHAR(50)     NOT NULL
    );
END
GO

-- 2. Table-Valued Type for Journal Entry Line insert
IF TYPE_ID('dbo.JournalEntryLineInsertType') IS NULL
BEGIN
    CREATE TYPE dbo.JournalEntryLineInsertType AS TABLE (
        JournalEntryId     BIGINT           NOT NULL,
        QBLineId           NVARCHAR(50)     NULL,
        LineNum            INT              NULL,
        DetailType         NVARCHAR(50)     NULL,
        Description        NVARCHAR(MAX)    NULL,
        Amount             DECIMAL(18,2)    NOT NULL,
        PostingType        NVARCHAR(50)     NULL,
        AccountRefId       NVARCHAR(50)     NULL,
        AccountRefName     NVARCHAR(255)    NULL,
        EntityType         NVARCHAR(50)     NULL,
        EntityRefId        NVARCHAR(50)     NULL,
        EntityRefName      NVARCHAR(255)    NULL,
        ProjectRefId       NVARCHAR(50)     NULL,
        RawLineJson        NVARCHAR(MAX)    NULL
    );
END
GO

-- 3. Stored procedure for Journal Entry Header upsert
CREATE OR ALTER PROCEDURE dbo.UpsertJournalEntryHeader
    @Headers dbo.JournalEntryHeaderUpsertType READONLY
AS
BEGIN
    SET NOCOUNT OFF;

    MERGE dbo.QBOJournalEntryHeader AS target
    USING @Headers AS source
    ON target.QBJournalEntryId = source.QBJournalEntryId
       AND target.QBRealmId = source.QBRealmId
    WHEN MATCHED THEN
        UPDATE SET
            SyncToken = source.SyncToken,
            Domain = source.Domain,
            TxnDate = source.TxnDate,
            Sparse = source.Sparse,
            Adjustment = source.Adjustment,
            CreateTime = source.CreateTime,
            LastUpdatedTime = source.LastUpdatedTime,
            RawJson = source.RawJson
    WHEN NOT MATCHED THEN
        INSERT (
            QBJournalEntryId, SyncToken, Domain, TxnDate, Sparse, Adjustment,
            CreateTime, LastUpdatedTime, RawJson, QBRealmId
        )
        VALUES (
            source.QBJournalEntryId, source.SyncToken, source.Domain, source.TxnDate,
            source.Sparse, source.Adjustment, source.CreateTime,
            source.LastUpdatedTime, source.RawJson, source.QBRealmId
        );
END;
GO

-- 4. Stored procedure for Journal Entry Line insert
CREATE OR ALTER PROCEDURE dbo.InsertJournalEntryLines
    @Lines dbo.JournalEntryLineInsertType READONLY
AS
BEGIN
    SET NOCOUNT OFF;

    INSERT INTO dbo.QBOJournalEntryLine
    (
        JournalEntryId, QBLineId, LineNum, DetailType, Description,
        Amount, PostingType,
        AccountRefId, AccountRefName,
        EntityType, EntityRefId, EntityRefName,
        ProjectRefId, RawLineJson
    )
    SELECT
        JournalEntryId, QBLineId, LineNum, DetailType, Description,
        Amount, PostingType,
        AccountRefId, AccountRefName,
        EntityType, EntityRefId, EntityRefName,
        ProjectRefId, RawLineJson
    FROM @Lines;
END;
GO
