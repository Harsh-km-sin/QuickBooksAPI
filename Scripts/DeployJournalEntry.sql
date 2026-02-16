-- =============================================================================
-- Journal Entry: TVPs and stored procedures
-- Run this script against your database so Journal Entry sync works.
--
-- Headers: The app uses a direct MERGE in code (no header proc required).
-- Lines:   The app calls dbo.InsertJournalEntryLines with TVP - REQUIRED.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Drop header proc/type first (so we can recreate type with more columns)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.UpsertJournalEntryHeader', 'P') IS NOT NULL
    DROP PROCEDURE dbo.UpsertJournalEntryHeader;
GO
IF TYPE_ID('dbo.JournalEntryHeaderUpsertType') IS NOT NULL
    DROP TYPE dbo.JournalEntryHeaderUpsertType;
GO

-- -----------------------------------------------------------------------------
-- 2. Header TVP (optional – app uses direct MERGE; kept for schema consistency)
-- -----------------------------------------------------------------------------
CREATE TYPE dbo.JournalEntryHeaderUpsertType AS TABLE (
    QBJournalEntryId   NVARCHAR(50)    NOT NULL,
    SyncToken          NVARCHAR(50)    NULL,
    Domain             NVARCHAR(50)    NULL,
    TxnDate            DATETIME2       NULL,
    Sparse             BIT             NULL,
    Adjustment         BIT             NULL,
    DocNumber          NVARCHAR(50)    NULL,
    PrivateNote        NVARCHAR(MAX)   NULL,
    CurrencyCode       NVARCHAR(10)    NULL,
    ExchangeRate       DECIMAL(18,6)   NULL,
    TotalAmount        DECIMAL(18,2)   NULL,
    HomeTotalAmount    DECIMAL(18,2)   NULL,
    CreateTime         DATETIMEOFFSET  NULL,
    LastUpdatedTime    DATETIMEOFFSET  NULL,
    RawJson            NVARCHAR(MAX)   NULL,
    QBRealmId          NVARCHAR(50)   NOT NULL
);
GO

-- -----------------------------------------------------------------------------
-- 3. Header stored procedure (optional – app uses direct MERGE)
-- -----------------------------------------------------------------------------
CREATE PROCEDURE dbo.UpsertJournalEntryHeader
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
            DocNumber = source.DocNumber,
            PrivateNote = source.PrivateNote,
            CurrencyCode = source.CurrencyCode,
            ExchangeRate = source.ExchangeRate,
            TotalAmount = source.TotalAmount,
            HomeTotalAmount = source.HomeTotalAmount,
            CreateTime = source.CreateTime,
            LastUpdatedTime = source.LastUpdatedTime,
            RawJson = source.RawJson
    WHEN NOT MATCHED THEN
        INSERT (
            QBJournalEntryId, SyncToken, Domain, TxnDate, Sparse, Adjustment,
            DocNumber, PrivateNote, CurrencyCode, ExchangeRate, TotalAmount, HomeTotalAmount,
            CreateTime, LastUpdatedTime, RawJson, QBRealmId
        )
        VALUES (
            source.QBJournalEntryId, source.SyncToken, source.Domain, source.TxnDate,
            source.Sparse, source.Adjustment,
            source.DocNumber, source.PrivateNote, source.CurrencyCode, source.ExchangeRate,
            source.TotalAmount, source.HomeTotalAmount,
            source.CreateTime, source.LastUpdatedTime, source.RawJson, source.QBRealmId
        );
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Line TVP – REQUIRED for Journal Entry sync (line insert)
-- -----------------------------------------------------------------------------
IF TYPE_ID('dbo.JournalEntryLineInsertType') IS NULL
BEGIN
    CREATE TYPE dbo.JournalEntryLineInsertType AS TABLE (
        JournalEntryId   BIGINT           NOT NULL,
        QBLineId         NVARCHAR(50)     NULL,
        LineNum          INT              NULL,
        DetailType       NVARCHAR(50)     NULL,
        Description      NVARCHAR(MAX)    NULL,
        Amount           DECIMAL(18,2)    NOT NULL,
        PostingType      NVARCHAR(50)     NULL,
        AccountRefId     NVARCHAR(50)     NULL,
        AccountRefName   NVARCHAR(255)    NULL,
        EntityType       NVARCHAR(50)     NULL,
        EntityRefId      NVARCHAR(50)     NULL,
        EntityRefName    NVARCHAR(255)    NULL,
        ProjectRefId     NVARCHAR(50)     NULL,
        RawLineJson      NVARCHAR(MAX)    NULL
    );
END
GO

-- -----------------------------------------------------------------------------
-- 5. Line insert stored procedure – REQUIRED for Journal Entry sync
-- -----------------------------------------------------------------------------
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
