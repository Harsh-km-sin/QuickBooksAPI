CREATE TABLE dbo.Companies
(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    UserId            INT NOT NULL,
    QboRealmId        NVARCHAR(50) NOT NULL,
    CompanyName       NVARCHAR(200),
    QboAccessToken    NVARCHAR(MAX),
    QboRefreshToken   NVARCHAR(MAX),
    TokenExpiryUtc    DATETIMEOFFSET,
    IsQboConnected    BIT NOT NULL DEFAULT 0,
    ConnectedAtUtc    DATETIMEOFFSET,
    DisconnectedAtUtc DATETIMEOFFSET,
    CreatedAtUtc      DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAtUtc      DATETIMEOFFSET,
    CONSTRAINT UQ_Companies_UserId_QboRealmId UNIQUE (UserId, QboRealmId)
);

CREATE INDEX IX_Companies_UserId ON dbo.Companies(UserId);

