-- Deploy order: 1) TVP first, 2) Stored procedure second

-- 1. Table-Valued Type for ChartOfAccounts upsert
IF TYPE_ID('dbo.ChartOfAccountsUpsertType') IS NULL
BEGIN
    CREATE TYPE dbo.ChartOfAccountsUpsertType AS TABLE (
        QBOId                       NVARCHAR(50)   NOT NULL,
        Name                        NVARCHAR(255)  NOT NULL,
        SubAccount                  BIT            NOT NULL,
        FullyQualifiedName          NVARCHAR(255)  NOT NULL,
        Active                      BIT            NOT NULL,
        Classification              NVARCHAR(50)   NULL,
        AccountType                 NVARCHAR(50)   NULL,
        AccountSubType              NVARCHAR(50)   NULL,
        CurrentBalance              DECIMAL(18,2)  NOT NULL,
        CurrentBalanceWithSubAccounts DECIMAL(18,2) NOT NULL,
        CurrencyRefValue            NVARCHAR(50)   NULL,
        CurrencyRefName             NVARCHAR(255)  NULL,
        Domain                      NVARCHAR(50)   NULL,
        Sparse                      BIT            NOT NULL,
        SyncToken                   NVARCHAR(50)   NOT NULL,
        CreateTime                  DATETIME2      NOT NULL,
        LastUpdatedTime             DATETIME2      NOT NULL,
        UserId                      INT            NOT NULL,
        RealmId                     NVARCHAR(50)   NOT NULL
    );
END
GO

-- 2. Stored procedure for ChartOfAccounts upsert
CREATE OR ALTER PROCEDURE dbo.UpsertChartOfAccounts
    @Accounts dbo.ChartOfAccountsUpsertType READONLY
AS
BEGIN
    SET NOCOUNT OFF;  -- Required for Dapper ExecuteAsync to return row count

    MERGE dbo.ChartOfAccounts AS target
    USING @Accounts AS source
    ON target.QBOId = source.QBOId 
       AND target.UserId = source.UserId 
       AND target.RealmId = source.RealmId
    WHEN MATCHED THEN
        UPDATE SET
            Name = source.Name,
            SubAccount = source.SubAccount,
            FullyQualifiedName = source.FullyQualifiedName,
            Active = source.Active,
            Classification = source.Classification,
            AccountType = source.AccountType,
            AccountSubType = source.AccountSubType,
            CurrentBalance = source.CurrentBalance,
            CurrentBalanceWithSubAccounts = source.CurrentBalanceWithSubAccounts,
            CurrencyRefValue = source.CurrencyRefValue,
            CurrencyRefName = source.CurrencyRefName,
            Domain = source.Domain,
            Sparse = source.Sparse,
            SyncToken = source.SyncToken,
            CreateTime = source.CreateTime,
            LastUpdatedTime = source.LastUpdatedTime
    WHEN NOT MATCHED THEN
        INSERT (
            QBOId, Name, SubAccount, FullyQualifiedName, Active, Classification, AccountType, AccountSubType,
            CurrentBalance, CurrentBalanceWithSubAccounts, CurrencyRefValue, CurrencyRefName, Domain, Sparse, SyncToken,
            CreateTime, LastUpdatedTime, UserId, RealmId
        )
        VALUES (
            source.QBOId, source.Name, source.SubAccount, source.FullyQualifiedName, source.Active, source.Classification,
            source.AccountType, source.AccountSubType, source.CurrentBalance, source.CurrentBalanceWithSubAccounts,
            source.CurrencyRefValue, source.CurrencyRefName, source.Domain, source.Sparse, source.SyncToken,
            source.CreateTime, source.LastUpdatedTime, source.UserId, source.RealmId
        );
END;
GO
