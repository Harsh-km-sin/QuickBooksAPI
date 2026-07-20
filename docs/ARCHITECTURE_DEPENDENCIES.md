# Allowed dependency direction (Phase 2)

This document is the agreed high-level graph for the QuickBooks API solution. Enforce incrementally with architecture tests and code review.

## Layers

| Area | May reference | Must not reference |
|------|----------------|-------------------|
| `QuickBooksAPI.Controllers` | Application interfaces, DTOs, infrastructure registrations via DI | Data access implementation details (`SqlClient`, raw SQL) except through injected abstractions |
| `QuickBooksAPI.Application` (interfaces) | Domain-style models shared with app, DTO contracts | `Microsoft.Data.SqlClient`, `QuickBooksAPI.Controllers` |
| `QuickBooksAPI.Services` (incl. feature subfolders e.g. `Services/Customers`) | Application interfaces, data interfaces (`I*Repository`), QuickBooks HTTP adapters | `QuickBooksAPI.Controllers` |
| `QuickBooksAPI.Features` (vertical slices e.g. `Features/Forecast`, `Features/CloseIssues`) | Application interfaces, DTOs, `I*Repository` | `QuickBooksAPI.Controllers`, `Microsoft.Data.SqlClient` |
| `QuickBooksAPI.DataAccessLayer` | Dapper, `Microsoft.Data.SqlClient` (via `ISqlConnectionFactory` for new code), models | `QuickBooksAPI.Controllers` |
| `QuickBooksService` | HTTP / Intuit SDK concerns | `QuickBooksAPI.Controllers` |
| `SyncWorker` | QuickBooksAPI application + data registrations | Controllers |
| `QuickBooksShared` (Contracts) | Options / cross-host config shapes only | API-specific types |

## Phase 2 data access

- New and migrated repositories should obtain connections from `ISqlConnectionFactory` (backed by `DatabaseOptions`).
- Prefer `ISqlExecutor` for single round-trip commands when cancellation must be threaded consistently.

## Request and sync context (Phase 2.3)

- **`IRequestContext`** (`UserId`, `RealmId`, `CorrelationId`): scoped per HTTP request. Populated in `CurrentUserMiddleware` after JWT validation; `CorrelationId` comes from `CorrelationIdMiddleware` (`HttpContext.Items` / `X-Correlation-Id`). Prefer injecting this over reading ambient user state when adding or refactoring API services.
- **`ISyncContext`**: scoped per worker operation. For full sync, populated in `FullSyncOrchestrator` from `FullSyncMessage` (optional `CorrelationId` on the message for end-to-end tracing with the publisher).
- **Worker host:** `SyncWorker` registers the same scoped `SyncContext` as both `ISyncContext` and `IRequestContext` so application services shared with the API resolve with one populated scope per message. **`ICurrentUser` was removed**; use **`IRequestContext`** in API and worker.
- **Full sync completion (optional decoupling):** after status is set to completed or partially failed, `FullSyncOrchestrator` invokes registered **`IFullSyncCompletedSubscriber`** implementations (exceptions are logged; orchestration still completes). Add subscribers via DI instead of editing the orchestrator for low-risk side effects (metrics, cache invalidation, etc.).

## Coupling hotspots (Phase 2.4 snapshot)

High fan-in types to watch when refactoring (many direct dependents or namespaces touched):

- `QuickBooksAPI.Controllers.AnalyticsController` — analytics read paths use `IAnomalyReadService` and `IConsolidationAnalyticsService` (no direct `Repos` types). Architecture test: `Controllers_ShouldNotDependOn_DataAccessLayer_Repos`.
- `QuickBooksAPI.DataAccessLayer.Repos.FinancialWarehouseRepository` — large SQL surface; batch rebuild script lives in `DataAccessLayer/Sql/FinancialWarehouseRebuildFactsSql.cs`.
- `QuickBooksAPI.Services.AuthServices` — thin `IAuthService` façade; logic in `Services/Auth/*` (`UserRegistrationService`, `UserLoginService`, `QboConnectionService`, `QboTokenLifecycleService`, `ConnectedCompanyQueryService`). Change with regression coverage.

## New API endpoints (Phase 2.5 checklist)

- Action signatures use **request/response DTOs** only; avoid `DataAccessLayer` entity types in public controller signatures for new code.
- Persistence goes through **injected application or domain-facing services**, not ad hoc `SqlConnection` in controllers or services.

## Warehouse analytics policy (Phase 2.5)

- COGS proxy and similar assumptions: **`WarehouseAnalyticsPolicy`** (`DataAccessLayer/Warehouse/WarehouseAnalyticsPolicy.cs`). Rebuild SQL references it via **`FinancialWarehouseRebuildFactsSql.Build`**.

## Data access registration

- All `I*Repository` implementations in `QuickBooksAPI.DataAccessLayer.Repos` use `ISqlConnectionFactory` (backed by `DatabaseOptions`).
- `IFinancialWarehouseRepository` is registered as **singleton** in `AddInfrastructure` (same connection factory; each operation opens its own connection).
