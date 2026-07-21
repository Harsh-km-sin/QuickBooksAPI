# AI Readiness Improvement Roadmap - QuickBooksAPI

## Current State Summary (kept current)

**Authoritative phase checklists:** `PHASE0_IMPLEMENTATION_TRACKER.md` → `PHASE3_IMPLEMENTATION_TRACKER.md`, plus `ARCHITECTURE_DEPENDENCIES.md` and `AI_GUIDELINES.md`.

### Project context
- **Solution:** `QuickBooksAPI.sln`
- **Backend:** .NET 8 ASP.NET Core Web API (`QuickBooksAPI`)
- **Integration library:** .NET 8 `QuickBooksService` (QuickBooks HTTP / OAuth helpers)
- **Worker:** Azure Functions isolated worker (`SyncWorker`) — Service Bus trigger for full sync
- **Frontend:** React + Vite + TypeScript (`QuickBooksAPI Frontend/app`)
- **Data access:** Dapper + SQL Server; repositories under `DataAccessLayer/Repos`
- **Auth:** JWT for app users; QuickBooks OAuth callback and token storage; see **Auth (implemented split)** below
- **Messaging:** Azure Service Bus (e.g. `qbo-full-sync` per typed `ServiceBusOptions`)

### Tests and CI (implemented)
- **Test projects**
  - `tests/ArchitectureTests` — NetArch rules (e.g. controllers ↔ `DataAccessLayer.Repos`, `SqlClient` boundaries), plus SQL data-access characterization tests (`SqlDataAccessTests`).
  - `tests/QuickBooksAPI.UnitTests` — focused unit tests (e.g. auth slices under `Auth/`, analytics where added). Run: `dotnet test QuickBooksAPI.sln -c Release`.
- **CI:** `.github/workflows/ci.yml` — `dotnet restore/build`, **`dotnet format --verify-no-changes`**, **`dotnet test`** on the solution, .NET package vulnerability listing, frontend `npm ci` / lint / build / audit, **strict file-size** checks (`tests/scripts/check-file-size.ps1`), **Gitleaks** on PR/push to `main`/`master`.

### Auth (implemented split)
- `IAuthService` is implemented by a **thin façade** `AuthServices` delegating to focused types in `QuickBooksAPI/Services/Auth/`:
  - `IUserRegistrationService`, `IUserLoginService`, `IQboConnectionService`, `IQboTokenLifecycleService` (QBO access tokens — not app JWT), `IConnectedCompanyQueryService`
- Field rules for sign-up live in `IUserSignUpValidator` / `UserSignUpRequestValidator`.
- Controllers still depend on `IAuthService` for compatibility; worker registers the same slices + façade.

### Data access (Phase 2 baseline in place)
- **`ISqlConnectionFactory`** + **`DatabaseOptions`** (command timeout); repositories migrated to factory-based connections; shared `AddSqlDataAccess()` used by API and worker.
- **`ISqlExecutor`** available for consistent command definition/timeouts.
- Warehouse: large rebuild SQL extracted (`FinancialWarehouseRebuildFactsSql`), policy constants in `WarehouseAnalyticsPolicy`; **`FinancialWarehouseRepository`** remains a **high-churn / high-fan-in** hotspot.
- SQL conventions documented: `DataAccessLayer/SQL_CONVENTIONS.md`.

### Explicit context (Phase 2.3)
- **`IRequestContext`** / **`RequestContext`** (API middleware) and **`ISyncContext`** / **`SyncContext`** (worker per message). **`ICurrentUser` / ambient user types removed** — use injected context.

### Worker shape (Phase 2.2)
- **`FullSyncWorker`** is a thin trigger; pipeline lives in **`IFullSyncOrchestrator` / `FullSyncOrchestrator`** with declarative steps.
- Optional: **`IFullSyncCompletedSubscriber`** for in-process completion hooks (e.g. logging).

### Frontend API surface (modularized)
- Shared HTTP/token helpers: `Frontend/app/src/api/core.ts`.
- Per-domain modules (`*Api.ts`) + thin `api/client.ts` barrel; feature imports can target modules directly or the barrel.

### Dominant pattern (evolving)
- **Layered monolith** still covers most of the API (`Controllers`, `Services`, `Repos`), but **Auth** and **Companies** now have **vertical slices** (`QuickBooksAPI/Features/Auth`, `…/Features/Companies`) with matching frontend (`features/auth`, `features/company`, `authApi` / `companyApi`). Remaining endpoints and UI are still technical-folder first; **multi-agent parallel ownership** is improved for those two slices, not yet global.

### Remaining pressure points for AI-assisted work
- **`SyncWorker/Program.cs`:** small but not unified with the API into a single shared `AddApplicationStack()` (hosts differ by design: Functions vs Kestrel).
- **Queue contracts:** `FullSyncMessage` is versioned in `QuickBooksShared`; broader Phase 4 naming (`FullSyncRequestedV1`-style envelopes) is optional.
- **Frontend:** per-domain `*Api.ts` + thin `client.ts` barrel is in place; further import-only refactors are optional.
- **Operational parity:** DB/schema must still match code for analytics; **`verify-dev-prerequisites.ps1`** (CI: `-Ci`) and **`verify-phase3-objects.sql`** add automated structure/table checks beyond doc links alone.

---

## Main problems vs mitigations (living document)

Original audit issues are **not all open**. Use this table to see **what still hurts** vs what has been **addressed** (do not regress).

| # | Topic | Status | Notes |
|---|--------|--------|--------|
| 1 | Overloaded auth service | **Mitigated** | `Services/Auth/*` + façade; unit tests in `tests/QuickBooksAPI.UnitTests/Auth` |
| 2 | God classes for core entities | **Mitigated** | Customer/Vendor/Invoice/Bill split into `Services/<Area>/*` + thin façades |
| 3 | Worker orchestration in one class | **Mitigated** | `IFullSyncOrchestrator` / `FullSyncOrchestrator`; thin `FullSyncWorker` |
| 4 | Monolithic frontend API | **Partial** | `api/core.ts`, `analyticsApi.ts`, barrel `client.ts` — Phase 6 still targets full feature modules |
| 5 | DI sprawl across hosts | **Partial** | API host composition split into `Infrastructure/QuickBooksApiWebApplicationBuilderExtensions` + `QuickBooksApiWebApplicationExtensions`; repos still registered in `AddInfrastructure` |
| 6 | Magic string configuration | **Mitigated** | Typed options in `QuickBooksShared`; `IOptions<>` in consumers (see Phase 1 tracker) |
| 7 | SQL + policy in warehouse | **Partial** | `WarehouseAnalyticsPolicy`, extracted rebuild SQL; repository still large |
| 8 | Ambient user context | **Mitigated** | `IRequestContext` / `ISyncContext`; `ICurrentUser` removed |
| 9 | No architecture enforcement | **Mitigated** | `tests/ArchitectureTests` + CI `dotnet test`; NetArch rules on key edges |
| 10 | Technical folders vs features | **Partial** | Entity HTTP in `Features/*`; QBO adapter DI grouped under `Integrations/QuickBooks/` |

---

## Target Outcome (Baseline vs today vs desired)

### Baseline (original external audit snapshot)
- Layered technical folders with scattered feature logic.
- Overloaded `AuthServices`, monolithic worker orchestration, fat `client.ts`.
- String-based config and inconsistent data-access wiring in many repos.
- Little or no automated architecture enforcement.

### Today (2026 — after Phases 0–3 work + Auth/Companies vertical slice pilots)
- Typed options, SQL factory/executor baseline, explicit request/sync context, auth and entity service splits, architecture tests in CI, partial frontend API split, documented dependency graph.
- **Auth and Companies** end-to-end slices: backend `Features/Auth`, `Features/Companies`; frontend `features/auth`, `features/company`, `authApi` / `companyApi`; READMEs and barrels for ownership boundaries.
- **Mostly layered** layout with **feature-first HTTP** and **per-domain frontend API modules**; `FullSyncMessage` versioned; architecture tests enforce key boundaries (including `Integrations` → not `Features`).

### After (Desired AI-Ready end state)
- **Feature-first modular monolith** (`Features/<FeatureName>/...`) with clear ownership.
- **Integration abstraction layer** (`Integrations/Abstractions` + `Integrations/Providers/<Provider>`).
- **Thin entry points** (controllers/functions) delegating to command/query handlers.
- **Versioned message contracts** for queue communication.
- **Typed options/config validation** and shared DI modules.
- **Architecture tests + CI gates** enforcing dependency rules, size limits, and secrets scanning.
- Frontend API split into **core transport + feature modules**, reducing conflict surface.

### AI readiness score (qualitative)

| Lens | Original audit (approx.) | **Current (approx.)** | Target (roadmap end state) |
|------|--------------------------|-------------------------|----------------------------|
| Single agent / single stream | ~4.5/10 | **~8–8.5/10** | ~8–8.5/10 |
| Multi-agent / parallel work | ~4/10 | **~7.5–8/10** | ~8/10 |

**Current drivers (strengths):** service decomposition, auth tests, architecture tests in CI, explicit context, typed config, documented boundaries; **vertical slices** across auth, companies, and **entity/analytics HTTP** (`Features/*` controllers); **frontend** `client.ts` is a thin barrel with **per-domain `*Api.ts`** files; **`Features_ShouldNotDependOn_DataAccessLayer_Repos`** satisfied via `Application.Interfaces` + warehouse row types in `Models`; **`FullSyncMessage`** in `QuickBooksShared` with **`SchemaVersion`** and worker version gate; **API host** slim `Program.cs` with **Infrastructure** extensions for JWT, Swagger, CORS, rate limiting, health; **QuickBooks Online** adapter registrations in **`Integrations/QuickBooks`**; optional **DB readiness** via **`HealthChecks:IncludeDatabase`**; **operational parity** scripts (`verify-dev-prerequisites.ps1`, `verify-phase3-objects.sql`) + doc links in CI.

**Still limiting parallel AI:** large **`AddInfrastructure`** repository list; **multi-provider** `Integrations/Abstractions` (e.g. Xero) not started; **no Testcontainers/SQL** in default CI; further gains from **command/query handlers** and splitting worker/API shared composition if desired.

---

## Problem -> Solution Mapping

### 1) Overloaded Auth Service
- **Problem:** `AuthServices` mixes signup/login, OAuth callback, token lifecycle, disconnect, company query.
- **Solution:** Split into focused services:
  - `IUserRegistrationService`
  - `IUserLoginService`
  - `IQboConnectionService`
  - `IQboTokenLifecycleService` (QBO access tokens)
  - `IConnectedCompanyQueryService`
  - plus `IUserSignUpValidator` for field rules
- **Implemented (Phase 3):** Thin `AuthServices` façade + `Services/Auth/*`; unit tests under `tests/QuickBooksAPI.UnitTests/Auth`.
- **Outcome:** Safer isolated edits and easier targeted tests.

### 2) Overloaded Full Sync Worker
- **Problem:** Trigger handling, orchestration, retry, entity sync, and status updates in one class.
- **Solution:** Extract:
  - `IFullSyncOrchestrator`
  - `ISyncStep` strategies (`CustomerSyncStep`, `VendorSyncStep`, etc.)
  - separate retry policy service
- **Implemented (Phase 2.2):** Thin `FullSyncWorker` + `FullSyncOrchestrator` with declarative `FullSyncEntityStep` list; optional `IFullSyncCompletedSubscriber`. Per-entity `ISyncStep` classes and dedicated retry service remain **future hardening** (Phase 4).
- **Outcome:** Each sync step is independently modifiable and testable.

### 3) Monolithic Frontend API Client
- **Problem:** One file owns transport + auth + all endpoint modules.
- **Solution:** Split into:
  - `api/core/httpClient.ts`
  - `api/core/authStorage.ts`
  - `api/modules/<feature>.ts`
- **Outcome:** Parallel-safe frontend changes with lower merge conflicts.

### 4) DI Duplication Across Hosts
- **Problem:** API and worker register many shared dependencies separately.
- **Solution:** Shared registration modules:
  - `AddPersistence()`
  - `AddApplicationServices()`
  - `AddIntegrations()`
  - `AddQueueing()`
- **Outcome:** One source of truth for registrations, less drift.

### 5) Magic String Configuration
- **Problem:** scattered `_config["QuickBooks:..."]`, `_config["Jwt:..."]`, etc.
- **Solution:** typed options + startup validation.
- **Implemented (Phase 1):** `QuickBooksShared` options (`QuickBooksOptions`, `JwtOptions`, `ServiceBusOptions`, `DatabaseOptions`, etc.) and `IOptions<>` consumers; see `PHASE1_IMPLEMENTATION_TRACKER.md`.
- **Outcome:** predictable, discoverable, and safer config evolution.

### 6) SQL and Business Logic Entanglement
- **Problem:** business assumptions (for example fixed COGS) embedded in repository SQL.
- **Solution:** policy extraction to application/domain + query object boundaries.
- **Partially implemented:** `WarehouseAnalyticsPolicy`, `FinancialWarehouseRebuildFactsSql`; `FinancialWarehouseRepository` still central and large.
- **Outcome:** business rule edits become local and testable.

### 7) Hidden Context Coupling
- **Problem:** runtime context is ambient and mutable.
- **Solution:** pass explicit request/sync context objects through handlers.
- **Implemented (Phase 2.3):** `IRequestContext` / `ISyncContext`; `ICurrentUser` removed.
- **Outcome:** clearer dependencies and safer concurrency.

### 8) No Enforced Architecture Rules
- **Problem:** boundaries are convention-only.
- **Solution:** architecture tests + CI failure gates.
- **Implemented (Phase 2.4+):** `tests/ArchitectureTests` run in CI; see `DependencyRulesTests` (`UnitTest1.cs`), `SqlDataAccessTests.cs`, and `ARCHITECTURE_DEPENDENCIES.md`.
- **Outcome:** prevents boundary erosion during AI-assisted coding.

---

## Target Architecture (Text Diagram)

```text
Client Layer (React feature modules, typed API modules)
    |
    v
API Entry Layer (thin controllers / function triggers)
    |
    v
Application Layer (commands, queries, handlers, orchestrators)
    |                         \
    |                          -> Integrations Abstractions
    v
Domain Policies/Models (pure business concepts)
    |
    v
Infrastructure (Dapper repos, SQL executors, queue publisher, identity adapters)
    |
    v
Data Stores / Service Bus / External Providers

Cross-cutting injected via DI:
Logging, Auth, Config, Correlation, Health, Telemetry
```

---

## Full Phased Implementation Plan and TODOs

### Phases 0–3 (status: largely shipped)

Implementation work for **Phases 0 through 3** is **mostly complete** in this repository. **Use the phase tracker files as the source of truth** for what is done vs still open:

| Phase | Tracker |
|-------|---------|
| 0 — Foundation, CI, governance | `PHASE0_IMPLEMENTATION_TRACKER.md` |
| 1 — Typed options / config | `PHASE1_IMPLEMENTATION_TRACKER.md` |
| 2 — Data access, context, coupling, worker extract | `PHASE2_IMPLEMENTATION_TRACKER.md` |
| 3 — Auth split + optional product/analytics items | `PHASE3_IMPLEMENTATION_TRACKER.md` |

The **checkbox lists in the sections below** are the **original roadmap backlog**. They are **not automatically kept in sync** — agents should **not** assume an unchecked box means work is missing without reading the tracker.

---

## Phase 0 - Foundation and Guardrails (Week 1)
**Goal:** Create migration-safe base and governance controls.

### TODOs
- [ ] Add scaffold folders:
  - [ ] `Features/`
  - [ ] `Integrations/`
  - [ ] `Contracts/`
  - [ ] `tests/`
- [ ] Add `AI_GUIDELINES.md` with:
  - [ ] safe edit zones
  - [ ] high-risk shared files
  - [ ] prohibited changes without human review
- [ ] Add architecture test project (NetArchTest/ArchUnitNET)
- [ ] Add initial CI pipeline with:
  - [ ] lint/format checks
  - [ ] unit test run
  - [ ] secrets scan (`gitleaks`)
  - [ ] dependency vulnerability scan
- [ ] Add baseline file-size governance script (warn mode)
- [ ] Record baseline metrics and smoke test list

---

## Phase 1 - Config and DI Consolidation (Weeks 2-3)
**Goal:** Remove configuration magic strings and DI drift.

### TODOs
- [ ] Create typed config classes:
  - [ ] `JwtOptions`
  - [ ] `QuickBooksOptions`
  - [ ] `ServiceBusOptions`
  - [ ] `AzureOpenAiOptions`
  - [ ] `RateLimitOptions`
- [ ] Bind and validate all options at startup (`ValidateOnStart`)
- [ ] Replace string indexer config usage in:
  - [ ] `QuickBooksAPI/Program.cs`
  - [ ] `QuickBooksAPI/Services/AuthServices.cs`
  - [ ] `QuickBooksService/Services/QuickBooksAuthService.cs`
  - [ ] other `QuickBooksService` provider classes
- [ ] Create reusable registration modules:
  - [ ] `AddPersistence()`
  - [ ] `AddApplicationServices()`
  - [ ] `AddIntegrations()`
  - [ ] `AddQueueing()`
- [ ] Refactor API and Worker program files to shared registration modules
- [ ] Add DI smoke tests for both hosts

---

## Phase 2 - Data Access Standardization (Weeks 4-5)
**Goal:** Make repository behavior consistent and AI-safe.

### TODOs
- [ ] Add `ISqlConnectionFactory`
- [ ] Add `ISqlExecutor` abstraction
- [ ] Add standard query/command timeout policy
- [ ] Migrate pilot repositories:
  - [ ] `CompanyRepository`
  - [ ] `TokenRepository`
  - [ ] `SyncStatusRepository`
- [ ] Add characterization tests for migrated repositories
- [ ] Define SQL conventions:
  - [ ] inline SQL max length threshold
  - [ ] query object pattern
  - [ ] parameterization rules

---

## Phase 3 - Break First God Class (Auth) (Weeks 6-7)
**Goal:** Decompose `AuthServices` into coherent modules.

### TODOs
- [ ] Extract interfaces and classes:
  - [ ] `IUserRegistrationService`
  - [ ] `IUserLoginService`
  - [ ] `IQboConnectionService`
  - [ ] `ITokenLifecycleService`
  - [ ] `IConnectedCompanyQueryService`
- [ ] Keep compatibility facade `IAuthService` during migration
- [ ] Move validation to dedicated validators/policies
- [ ] Move token refresh/expiry to token lifecycle class
- [ ] Add tests:
  - [ ] register/login
  - [ ] oauth callback
  - [ ] token refresh
  - [ ] disconnect
  - [ ] connected companies query

---

## Phase 4 - Worker and Messaging Decoupling (Weeks 8-9)
**Goal:** Make sync pipeline modular and parallel-safe.

### TODOs
- [ ] Create versioned queue contracts:
  - [ ] `FullSyncRequestedV1`
- [ ] Refactor worker into:
  - [ ] thin trigger handler
  - [ ] `IFullSyncOrchestrator`
  - [ ] `ISyncStep` implementations for each entity
- [ ] Extract retry strategy into dedicated service
- [ ] Add poison/dead-letter handling policy
- [ ] Add sync flow integration tests with message fixtures

---

## Phase 5 - Vertical Slice Migration (Weeks 10-11)
**Goal:** Shift to feature ownership boundaries.

### TODOs
- [ ] Migrate `Companies` feature end-to-end:
  - [ ] API
  - [ ] commands/queries
  - [ ] contracts
  - [ ] handlers
- [ ] Migrate `Auth` feature end-to-end
- [ ] Add per-feature ownership docs (`README.md` under feature folders)
- [ ] Start next slices:
  - [ ] `Customers`
  - [ ] `Vendors`
- [ ] Enforce no direct cross-feature calls outside contracts/events

---

## Phase 6 - Frontend API Modularization and Final Governance (Week 12)
**Goal:** Reduce frontend conflict surface and lock governance.

### TODOs
- [ ] Split `Frontend/app/src/api/client.ts` into:
  - [ ] `api/core/httpClient.ts`
  - [ ] `api/core/authStorage.ts`
  - [ ] `api/modules/auth.ts`
  - [ ] `api/modules/company.ts`
  - [ ] `api/modules/customer.ts`
  - [ ] `api/modules/vendor.ts`
  - [ ] `api/modules/invoice.ts`
  - [ ] `api/modules/bill.ts`
  - [ ] `api/modules/analytics.ts`
  - [ ] `api/modules/assistant.ts`
- [ ] Update hooks/pages imports to feature API modules
- [ ] Add API contract validation tests
- [ ] Change governance checks from warn -> fail:
  - [ ] dependency rule violations fail build
  - [ ] file size limits fail build
  - [ ] secrets scan fail build

---

## Cross-Phase Continuous TODOs
- [ ] Write characterization tests before each major refactor seam
- [ ] Keep PRs small and feature-scoped
- [ ] Maintain temporary compatibility adapters during transitions
- [ ] Add change log entries for architectural moves
- [ ] Run weekly architecture review and drift check

---

## Dependency Rules to Enforce in CI

### Allowed
- API -> Application
- Application -> Domain + Integrations.Abstractions
- Infrastructure -> Application contracts + Domain
- Integrations.Providers -> Integrations.Abstractions
- Worker -> Application + Messaging contracts
- Tests -> Any

### Forbidden
- Domain -> Infrastructure
- Domain -> Integrations providers
- Application -> Integrations provider implementations
- Provider A -> Provider B
- Production code -> test assemblies

---

## Governance Standards

### Naming
- Feature folders: PascalCase nouns (`Auth`, `Companies`, `Analytics`)
- Interfaces: `I` + capability (`ICompanySyncService`)
- Commands/Queries: `VerbNounCommand`, `GetNounQuery`
- Provider implementations: `QuickBooks...Provider`, `Sql...Repository`
- Tests: `<ClassName>Tests`

### Size Limits
- Class/file: <= 300 lines
- Method: <= 30 lines
- Constructor params: <= 4 preferred
- Azure Function handler: <= 50 lines
- React component: <= 200 lines
- Inline SQL: <= 20 lines before query object extraction

---

## Expected End State
- Modular monolith organized by business capabilities.
- Integration-ready adapter architecture where new providers can be added without touching core business logic.
- Multiple AI agents can safely work in parallel on separate features/providers.
- Architecture quality protected by automated checks, not just conventions.

---

## Recommended First Sprint (2 Weeks)

**Superseded** — the items below were the **initial** sprint plan. Much of this has since landed (scaffolding, typed options, architecture tests, CI, auth decomposition, partial frontend `api/` split). **Use `PHASE0`–`PHASE3` trackers** and **Phases 4–6** below for current next steps.

Historical checklist (for archive context only):

- Add scaffolding (`Features`, `Integrations`, `Contracts`, `tests`)
- Add typed options and startup validation
- Extract shared DI registration modules
- Add architecture test project and baseline CI checks
- Begin `AuthServices` decomposition (first extraction only)
- Define and add `FullSyncRequestedV1` contract
- Start frontend API split with `httpClient.ts` + `auth.ts` + `company.ts`

