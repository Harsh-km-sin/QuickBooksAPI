INSERT INTO dbo.Companies (
    UserId,
    QboRealmId,
    CompanyName,
    QboAccessToken,
    QboRefreshToken,
    TokenExpiryUtc,
    IsQboConnected,
    ConnectedAtUtc,
    DisconnectedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    qt.UserId,
    qt.RealmId,
    NULL AS CompanyName,
    qt.AccessToken,
    qt.RefreshToken,
    DATEADD(SECOND, qt.ExpiresIn, qt.CreatedAt) AS TokenExpiryUtc,
    1 AS IsQboConnected,
    qt.CreatedAt AS ConnectedAtUtc,
    NULL AS DisconnectedAtUtc,
    qt.CreatedAt AS CreatedAtUtc,
    qt.UpdatedAt AS UpdatedAtUtc
FROM dbo.QuickBooksToken qt
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Companies c
    WHERE c.UserId = qt.UserId
      AND c.QboRealmId = qt.RealmId
);

