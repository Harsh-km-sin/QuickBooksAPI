# Phase 2 Implementation Tracker (AI-Ready Decoupling & Data Access)

## How to maintain this file
- **Check off** `[ ]` → `[x]` when a todo is done (same PR as the code change when possible).
- **Re-run and tick** the **Verified** section after meaningful backend changes.
- **Refresh LOC** in §2.2 if a service file changes size a lot (rough counts are enough).

## Next
- **Phase 3** (two tracks: Auth split vs product CFO/analytics scope): see [`PHASE3_IMPLEMENTATION_TRACKER.md`](PHASE3_IMPLEMENTATION_TRACKER.md).

## Scope
Phase 2 standardizes data access, shrinks god classes, and replaces hidden ambient context so multiple agents (and humans) can change code safely with clear boundaries.

**Suggested order:** **2.2** + **2.5** (warehouse SQL) while threading **2.3** into new code → **2.4** after splits slow down → **2.6** last / optional.

---

## Remaining work (master checklist)
Copy for sprint boards; keep in sync with sections below.

### Open
- [ ] **2.2** (execute) run full-sync E2E smoke in a real environment using [`tests/docs/FULLSYNC_WORKER_SMOKE.md`](tests/docs/FULLSYNC_WORKER_SMOKE.md) and confirm success criteria

### Done (summary)
- [x] **2.0** Guardrails + docs + `Application_ShouldNotDependOn_SqlClient`
- [x] **2.1** Factory, executor, all repos + warehouse registration
- [x] **2.1o** `ISqlConnectionFactory.CommandTimeoutSeconds` + `CreateCommand` extension; repos wired; `SqlExecutor` uses factory timeout only
- [x] **2.2** (partial) `CustomerService` split into `Services/Customers/*` + façade + DI (API + SyncWorker)
- [x] **2.2** (rest) `VendorService`, `InvoiceService`, `BillService` splits + `IFullSyncOrchestrator` / `FullSyncOrchestrator`; thin `FullSyncWorker`
- [x] **2.3** (core) `IRequestContext` / `ISyncContext`, API middleware + full-sync orchestration population; `CustomerService` on `IRequestContext`; `FullSyncMessage.CorrelationId`
- [x] **2.3** (follow-up) all façade services + analytics/company/CFO controllers on `IRequestContext`; removed `ICurrentUser` / `CurrentUser` / `SyncCurrentUser`
- [x] **2.4** Coupling appendix + NetArch: controllers + `Services*` → no `SqlClient`
- [x] **2.5** `FinancialWarehouseRebuildFactsSql` + `WarehouseAnalyticsPolicy`
- [x] **2.5** (optional) `AnalyticsController` → `IAnomalyReadService` / `IConsolidationAnalyticsService`; NetArch: `ApiControllers_ShouldNotDependOn_DataAccessLayer_Repos`
- [x] **2.6** (optional) `IFullSyncCompletedSubscriber` + `LoggingFullSyncCompletedSubscriber`; frontend `api/core.ts` + `api/analyticsApi.ts` barrel via `client.ts`

---

## Completed (this slice)
- [x] **2.0** Scope & guardrails (this batch): Phase 2 data-access criteria and dependency graph documented in `ARCHITECTURE_DEPENDENCIES.md`; architecture test `Application_ShouldNotDependOn_SqlClient`; SQL conventions in `QuickBooksAPI/DataAccessLayer/SQL_CONVENTIONS.md`.
- [x] **2.1** Data access core + follow-ups: all repositories use `ISqlConnectionFactory`; `IFinancialWarehouseRepository` registered in `AddInfrastructure` (singleton) with factory; `Program.cs` no longer special-cases warehouse; `SyncWorker` mirrors API registrations (`IFinancialWarehouseRepository` singleton). Shared: `ISqlConnectionFactory`, `SqlConnectionFactory`, `ISqlExecutor`, `SqlExecutor`, `AddSqlDataAccess()`; `DatabaseOptions` + `CommandTimeoutSeconds`; tests in `tests/ArchitectureTests/SqlDataAccessTests.cs`.
- [x] **2.1 optional:** `CommandTimeoutSeconds` on `ISqlConnectionFactory`; `SqlConnectionFactoryDapperExtensions.CreateCommand`; all listed repos + warehouse use it; `SqlExecutor` ctor is factory-only.
- [x] **2.2 (Customer):** `ICustomerReadService`, `ICustomerQboSyncService`, `ICustomerQboCommandService`; implementations under `QuickBooksAPI/Services/Customers/` (`CustomerReadService`, `CustomerQboSyncService`, `CustomerQboCommandService`, `QuickBooksCustomerMapper`, `CustomerRequestValidator`); thin `CustomerService` façade; `Program.cs` + `SyncWorker/Program.cs` registrations.

## Verified
- [x] `dotnet build QuickBooksAPI.sln` (0 errors)
- [x] `dotnet test QuickBooksAPI.sln -c Release` (pass: architecture + unit tests including forecast slice)
- [x] `dotnet build SyncWorker/SyncWorker.csproj -c Release` (0 errors)
- [x] Characterization checks: `SqlDataAccessTests` + NetArch rules

---

## 2.0 — Scope & guardrails
- [x] Define Phase 2 “done” criteria (e.g. pilot repos use factory; no new raw `SqlConnection` in migrated code paths) — see `ARCHITECTURE_DEPENDENCIES.md`
- [x] Extend architecture tests for additional forbidden edges (start with 1–3 rules) — added `Application_ShouldNotDependOn_SqlClient`
- [x] Document allowed dependency graph (API → Application → Domain; Infrastructure implements data access) — `ARCHITECTURE_DEPENDENCIES.md`

---

## 2.1 — Data access core
- [x] Add `ISqlConnectionFactory` (connection string + command timeout surface for Dapper)
- [x] Add `ISqlExecutor` (Dapper + `CommandDefinition` cancellation + command timeout via factory)
- [x] Register in `Infrastructure/DependencyInjection.cs` and align `SyncWorker` registration (`AddSqlDataAccess` extension)
- [x] Migrate pilot repositories:
  - [x] `TokenRepository`
  - [x] `CompanyRepository`
  - [x] `SyncStatusRepository`
- [x] Remove duplicated connection-string plumbing from migrated repos (constructors take `ISqlConnectionFactory`)
- [x] Add characterization tests (`SqlDataAccessTests`) for factory behavior
- [x] Document SQL conventions — `QuickBooksAPI/DataAccessLayer/SQL_CONVENTIONS.md`

### 2.1 optional — Cancellation + `DatabaseOptions` command timeout
- [x] `AnomalyEventRepository`
- [x] `KpiSnapshotRepository`
- [x] `ForecastScenarioRepository`
- [x] `ForecastResultRepository`
- [x] `CloseIssueRepository`
- [x] `DimEntityRepository`
- [x] `ConsolidatedPnlRepository`
- [x] `FinancialWarehouseRepository` (including `RebuildFactsAsync` cancellation + timeout)

---

## 2.2 — God classes & worker orchestration

**Inventory (updated):** `CustomerService.cs`, `VendorService.cs`, `InvoiceService.cs`, `BillService.cs` are thin façades; logic under `Services/Customers/*`, `Services/Vendors/*`, `Services/Invoices/*`, `Services/Bills/*`. `SyncWorker/FullSyncWorker.cs` is trigger + failure status; pipeline in `SyncWorker/FullSyncOrchestrator.cs` (per-entity steps via `BuildSteps()` + `FullSyncEntityStep`).

- [x] Inventory oversized services (`CustomerService`, `VendorService`, `InvoiceService`, `BillService`, `FullSyncWorker`, etc.)

### CustomerService
- [x] Map public responsibilities → read / QBO sync / QBO mutations
- [x] Interfaces: `ICustomerReadService`, `ICustomerQboSyncService`, `ICustomerQboCommandService`
- [x] Implementations in `QuickBooksAPI/Services/Customers/`; `ICustomerService` remains API façade
- [x] DI in `Program.cs` and `SyncWorker/Program.cs`

### VendorService
- [x] Same pattern as CustomerService (sync vs CRUD/paging vs mapping)

### InvoiceService
- [x] Same pattern (align naming with Customer/Vendor splits)

### BillService
- [x] Same pattern

### FullSyncWorker (`SyncWorker/FullSyncWorker.cs`)
- [x] Extract `IFullSyncOrchestrator` (or equivalent) — host registers implementation
- [x] Extract per-entity steps — declarative `FullSyncEntityStep` list in `FullSyncOrchestrator.BuildSteps()`
- [x] Leave function trigger thin (deserialize message, call orchestrator, complete message; failure status on exception)
- [x] E2E smoke **runbook** — [`tests/docs/FULLSYNC_WORKER_SMOKE.md`](tests/docs/FULLSYNC_WORKER_SMOKE.md)
- [ ] Execute runbook against staging/prod (Service Bus + SQL + QBO) and tick Phase 3 carry-over when passed

---

## 2.3 — Explicit context (reduce ambient globals)
- [x] Add `RequestContext` (or `SyncContext`) type: at least `UserId`, `RealmId`, `CorrelationId` — `IRequestContext` + `RequestContext`; `ISyncContext` + `SyncContext`
- [x] Register scoped context in API pipeline; populate from auth + headers where applicable — `Program.cs` + `CurrentUserMiddleware` + existing `CorrelationIdMiddleware` item key
- [x] Register/populate analogous context in `SyncWorker` from queue message payload — `FullSyncOrchestrator` sets `SyncContext` with each run
- [x] Migrate **one** vertical slice (e.g. one controller + its service) off direct `CurrentUser` reads to injected context — `CustomerService` uses `IRequestContext` (`CustomerController` had no `ICurrentUser` dependency)
- [x] Document pattern in `ARCHITECTURE_DEPENDENCIES.md` or short comment in middleware — see `ARCHITECTURE_DEPENDENCIES.md` § Request and sync context
- [x] Audit Service Bus / sync messages for required IDs; extend DTO if anything is missing — added optional `CorrelationId` on `FullSyncMessage`

---

## 2.4 — Dependency direction & coupling
- [x] Run coupling pass (script, IDE references, or NetArch “types that depend on many namespaces”) — capture top 10 in a short note (e.g. appendix in this file or `ARCHITECTURE_DEPENDENCIES.md`)
- [x] Agree 1–2 new rules (e.g. Controllers → no direct repo types; Services → no `SqlClient`) — implemented: controller + `Services*` → no `Microsoft.Data.SqlClient`
- [x] Add NetArchTest assertions in `tests/ArchitectureTests`
- [x] Re-run architecture tests; fix violations or document exceptions

---

## 2.5 — SQL & repository boundaries
- [x] `FinancialWarehouseRepository`: extract large SQL strings to named types or `.sql` embedded resources (no behavior change)
- [x] Identify magic numbers / COGS (or similar) policy in warehouse or services; move to options or static policy class
- [x] For **new** controllers: checklist — request/response DTOs only; no persistence models in action signatures
- [x] (Optional) Spot-check hot endpoints — `AnalyticsController` no longer references `DataAccessLayer.Repos`; architecture test guards controller layer

---

## 2.6 — Optional stretch
- [x] In-process hooks: `IFullSyncCompletedSubscriber` (`Application/Interfaces/FullSyncCompletion.cs`); `FullSyncOrchestrator` invokes subscribers after status update; sample `LoggingFullSyncCompletedSubscriber` in SyncWorker
- [x] Frontend pilot: `Frontend/app/src/api/core.ts` + `analyticsApi.ts`; `client.ts` re-exports so `@/api/client` imports stay stable
