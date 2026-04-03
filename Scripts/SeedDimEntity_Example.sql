-- Example: seed dim_entity for consolidation smoke tests.
-- Replace @UserId, realm ids, and names with real values from your app / QBO.
-- Parent row must be inserted first (FK from child to parent).

/*
DECLARE @UserId INT = 1;

-- 1) Optional consolidated parent
INSERT INTO dbo.dim_entity (UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode)
VALUES (@UserId, N'CONSOLIDATED-ROOT', NULL, N'All entities rollup', N'USD', 1);
DECLARE @ParentId INT = SCOPE_IDENTITY();

-- 2) Leaf = actual QBO realm
INSERT INTO dbo.dim_entity (UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode)
VALUES (@UserId, N'<your-qbo-realm-id>', @ParentId, N'Operating company', N'USD', 0);
*/

-- Minimal leaf-only row (no consolidation parent):
-- INSERT INTO dbo.dim_entity (UserId, RealmId, ParentEntityId, Name, Currency, IsConsolidatedNode)
-- VALUES (1, N'<your-qbo-realm-id>', NULL, N'My company', N'USD', 0);
