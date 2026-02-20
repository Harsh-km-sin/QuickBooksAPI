-- Optional: Use this if your schema has a Company table for QuickBooks connection.
-- The QuickBooksAPI app currently uses QuickBooksToken; disconnect deletes the token row.
-- If you use a Company table instead, run this after revoking the token to clear connection:

-- UPDATE Company
-- SET
--     QboAccessToken = NULL,
--     QboRefreshToken = NULL,
--     IsQboConnected = 0
-- WHERE CompanyId = @id;

-- Example with a specific company id (replace @id with your parameter or value):
-- UPDATE dbo.Company
-- SET
--     QboAccessToken = NULL,
--     QboRefreshToken = NULL,
--     IsQboConnected = 0
-- WHERE CompanyId = @CompanyId;
