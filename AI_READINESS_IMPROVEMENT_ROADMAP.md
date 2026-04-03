# AI Readiness Improvement Roadmap - QuickBooksAPI

## Current State Summary

### Project Context (as observed)
- **Solution:** `QuickBooksAPI.sln`
- **Backend:** .NET 8 ASP.NET Core Web API (`QuickBooksAPI`)
- **Integration Library:** .NET 8 class library (`QuickBooksService`) for QuickBooks HTTP interactions
- **Worker:** Azure Functions v4 (`SyncWorker`) with Service Bus trigger for full sync
- **Frontend:** React + Vite + TypeScript (`QuickBooksAPI Frontend/app`)
- **Data Access:** Dapper + SQL Server (`DataAccessLayer/Repos`)
- **Auth:** JWT + QuickBooks OAuth 2.0 callback flow
- **Messaging:** Azure Service Bus queue (`qbo-full-sync`)
- **Testing/CI:** No test project or CI workflow files were found during analysis

### Dominant Architectural Pattern
- Current style is a **layered monolith** with technical folders (`Controllers`, `Services`, `Repos`) plus:
  - external integration project (`QuickBooksService`)
  - asynchronous worker project (`SyncWorker`)
- This gives partial separation, but feature ownership is spread across multiple folders/projects.

### Folder/Layout Issues
- Feature logic is dispersed across:
  - `QuickBooksAPI/Controllers/*`
  - `QuickBooksAPI/Services/*`
  - `QuickBooksAPI/DataAccessLayer/Repos/*`
  - `QuickBooksService/Services/*`
  - `SyncWorker/*`
- `QuickBooksAPI/Program.cs` and `SyncWorker/Program.cs` both hold large DI/composition logic, creating drift risk.
- Frontend API usage is centralized in one high-churn file: `QuickBooksAPI Frontend/app/src/api/client.ts`.

### Data Access Pattern and Risks
- Pattern is **Repository + Dapper + raw SQL**, with direct `new SqlConnection(...)` in most repositories.
- Positive: repository interfaces exist.
- Risks:
  - repeated boilerplate and inconsistent patterns
  - large SQL blobs with business assumptions in repository code (`FinancialWarehouseRepository`)
  - more difficult safe edits for AI agents

### Frontend/Backend Coupling
- Backend depends on `X-Realm-Id` header and claim extraction in middleware.
- Frontend stores auth and realm context globally (`sessionStorage`, `localStorage`) and routes all APIs through one file.
- API contracts are typed in TS but not clearly versioned as a contract layer.

### Messaging/Worker Gaps
- Queue boundary exists, but message contract is not formally versioned.
- `FullSyncWorker` acts as both trigger handler and orchestration engine, increasing coupling and edit conflicts.

---

## Main Problems Hurting AI-Driven Development

| # | Problem | Where It Appears | AI Impact |
|---|---|---|---|
| 1 | God/overloaded service classes with mixed concerns | `Services/AuthServices.cs`, `CustomerService.cs`, `VendorService.cs`, `InvoiceService.cs` | AI cannot safely modify one responsibility without understanding unrelated logic in the same class |
| 2 | Overloaded worker orchestrator | `SyncWorker/FullSyncWorker.cs` | Parallel changes collide; one bugfix can affect multiple sync paths |
| 3 | Monolithic frontend API layer | `Frontend/app/src/api/client.ts` | Any API change creates broad conflict surface and high merge risk |
| 4 | DI duplication and composition sprawl | `QuickBooksAPI/Program.cs`, `SyncWorker/Program.cs`, `Infrastructure/DependencyInjection.cs` | AI must track multiple registration sites; drift causes runtime failures |
| 5 | Scattered string-based configuration | `_config["..."]` access in multiple services/programs | Magic strings are easy to copy incorrectly and hard to validate globally |
| 6 | SQL + business assumptions mixed in infra | `FinancialWarehouseRepository.cs` | Business logic is split between C# and SQL, reducing local comprehensibility |
| 7 | Ambient context coupling | `CurrentUserMiddleware`, `CurrentUser`, `SyncCurrentUser` | Hidden runtime dependencies make isolated edits/testing harder |
| 8 | Missing architecture test guardrails | No architecture test project/CI enforcement observed | AI and human refactors can silently violate boundaries |

---

## Target Outcome (Before vs After)

### Before (Current)
- Layered technical folders with scattered feature logic.
- Multiple overloaded classes (`AuthServices`, `FullSyncWorker`, `client.ts`).
- String-based config and duplicated DI registration.
- Data access patterns repeated across many repositories.
- Weak guardrails for dependency direction and AI-safe parallel edits.

### After (Desired AI-Ready)
- **Feature-first modular monolith** (`Features/<FeatureName>/...`) with clear ownership.
- **Integration abstraction layer** (`Integrations/Abstractions` + `Integrations/Providers/<Provider>`).
- **Thin entry points** (controllers/functions) delegating to command/query handlers.
- **Versioned message contracts** for queue communication.
- **Typed options/config validation** and shared DI modules.
- **Architecture tests + CI gates** enforcing dependency rules, size limits, and secrets scanning.
- Frontend API split into **core transport + feature modules**, reducing conflict surface.

### AI Readiness Score Projection
- **Single-AI readiness:** ~4.5/10 -> ~8.5/10
- **Multi-AI readiness:** ~4/10 -> ~8/10

Primary drivers:
- Vertical slice ownership
- Service decomposition
- Integration contract boundaries
- CI-enforced architecture governance

---

## Problem -> Solution Mapping

### 1) Overloaded Auth Service
- **Problem:** `AuthServices` mixes signup/login, OAuth callback, token lifecycle, disconnect, company query.
- **Solution:** Split into focused services:
  - `IUserRegistrationService`
  - `IUserLoginService`
  - `IQboConnectionService`
  - `ITokenLifecycleService`
  - `IConnectedCompanyQueryService`
- **Outcome:** Safer isolated edits and easier targeted tests.

### 2) Overloaded Full Sync Worker
- **Problem:** Trigger handling, orchestration, retry, entity sync, and status updates in one class.
- **Solution:** Extract:
  - `IFullSyncOrchestrator`
  - `ISyncStep` strategies (`CustomerSyncStep`, `VendorSyncStep`, etc.)
  - separate retry policy service
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
- **Outcome:** predictable, discoverable, and safer config evolution.

### 6) SQL and Business Logic Entanglement
- **Problem:** business assumptions (for example fixed COGS) embedded in repository SQL.
- **Solution:** policy extraction to application/domain + query object boundaries.
- **Outcome:** business rule edits become local and testable.

### 7) Hidden Context Coupling
- **Problem:** runtime context is ambient and mutable.
- **Solution:** pass explicit request/sync context objects through handlers.
- **Outcome:** clearer dependencies and safer concurrency.

### 8) No Enforced Architecture Rules
- **Problem:** boundaries are convention-only.
- **Solution:** architecture tests + CI failure gates.
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
- [ ] Add scaffolding (`Features`, `Integrations`, `Contracts`, `tests`)
- [ ] Add typed options and startup validation
- [ ] Extract shared DI registration modules
- [ ] Add architecture test project and baseline CI checks
- [ ] Begin `AuthServices` decomposition (first extraction only)
- [ ] Define and add `FullSyncRequestedV1` contract
- [ ] Start frontend API split with `httpClient.ts` + `auth.ts` + `company.ts`

