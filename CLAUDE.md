# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

A QuickBooks Online integration + CFO analytics platform made of **four independently deployed pieces** that share one SQL Server database and Azure Service Bus namespace:

| Deployable | Path | Stack | Role |
|---|---|---|---|
| Web API | `QuickBooksAPI/` | .NET 8 / ASP.NET Core | HTTP surface, JWT auth, QBO OAuth, analytics reads, GL upload |
| Sync worker | `SyncWorker/` | .NET 8 Azure Functions (isolated) | `qbo-full-sync` Service Bus trigger + daily/monthly timer jobs |
| GL analysis worker | `GlAnalysisWorker/` | Python 3.12 container (Azure Container Apps) | `gl-analysis` queue consumer, 9-phase anomaly pipeline |
| Frontend | `QuickBooksAPI Frontend/app/` | React 19 + Vite + TS + Tailwind + shadcn/ui | SPA client |

Shared class libraries: `QuickBooksService/` (Intuit HTTP/OAuth adapters) and `Contracts/QuickBooksShared/` (cross-host options + Service Bus message shapes).

`README.md` is the full backend/API reference (endpoint table, DTO shapes, entity models). `docs/` holds phase trackers, deployment runbooks, and the GL feature breakdown.

## Commands

Backend (from repo root):

```powershell
dotnet restore QuickBooksAPI.sln
dotnet build QuickBooksAPI.sln
dotnet run --project QuickBooksAPI/QuickBooksAPI.csproj      # https://localhost:7135, Swagger UI at /
dotnet test QuickBooksAPI.sln                                 # unit + architecture tests
dotnet test tests/QuickBooksAPI.UnitTests/QuickBooksAPI.UnitTests.csproj --filter "FullyQualifiedName~UserLoginServiceTests"
dotnet format QuickBooksAPI.sln --verify-no-changes           # CI gate; drop the flag to apply
```

Pre-push gate — run this before considering backend work done; it is what CI enforces:

```powershell
./tests/scripts/run-local-checks.ps1            # arch tests + file-size + dotnet format + frontend lint
./tests/scripts/run-local-checks.ps1 -SkipFrontend
./tests/scripts/install-git-hooks.ps1           # optional: wire the above to pre-commit
./tests/scripts/verify-dev-prerequisites.ps1    # DB/config sanity before smoke tests
```

Frontend (from `QuickBooksAPI Frontend/app/`):

```bash
npm ci && npm run dev      # http://localhost:5173
npm run lint               # eslint — CI gate
npm run build              # tsc -b && vite build — CI gate
```

GL worker (from `GlAnalysisWorker/`):

```bash
pip install -r requirements.txt
python main.py             # runs a stub loop if SERVICE_BUS_CONNECTION_STRING is unset
python -m compileall -q .  # CI's only static check
docker build -t gl-analysis-worker:ci .
```

OpenAPI contract: CI emits `artifacts/openapi.json` via `dotnet tool restore && dotnet swagger tofile`. Locally, `Scripts/export-openapi.ps1` boots the API under the `OpenApiExport` environment and writes `QuickBooksAPI Frontend/app/openapi/openapi-v1.json`, from which `src/api/generated/openapi.ts` is produced with `openapi-typescript`.

## Hard CI gates (`.github/workflows/ci.yml`)

- **300-line file limit**, fail-on-violation, over `QuickBooksAPI/`, `SyncWorker/`, `tests/`, and `app/src/` (shadcn `components/ui` excluded). This is why controllers/DI are split into `partial` classes and `*.Feature*ServiceCollectionExtensions.cs` files. **Split before growing a file**, don't append.
- `dotnet format --verify-no-changes` on the whole solution.
- Architecture tests (NetArchTest) — see below.
- Gitleaks secret scan; `dotnet list package --vulnerable`; `npm audit --omit=dev --audit-level=high`.

## Architecture rules (enforced by `tests/ArchitectureTests`)

Adding code that violates these breaks the build:

- `QuickBooksAPI.Features.*` and `QuickBooksAPI.Controllers.*` must **not** reference `QuickBooksAPI.DataAccessLayer.Repos` or `Microsoft.Data.SqlClient`. Repository **interfaces** go in `QuickBooksAPI/Application/Interfaces/`; implementations stay in `DataAccessLayer/Repos/`.
- Feature **handlers** (`Features/<X>/Handlers/`) must not reference `QuickBooksService.Services` directly — go through the gateway abstractions in `Integrations/Abstractions/` (implemented in `Integrations/QuickBooks/`).
- Migrated `Application/Interfaces` contracts (e.g. `IProductRepository`, `ICustomerReadService`, `IInvoiceService`) must not reference `DataAccessLayer.Models`; use `Application/Dtos`. Legacy interfaces still do — new/migrated ones must not, and each has a named test.
- Nothing outside controllers may depend on `QuickBooksAPI.Controllers`; `Integrations` must not depend on `Features`.

`docs/ARCHITECTURE_DEPENDENCIES.md` is the authoritative layer table.

## Key cross-cutting conventions

- **Every response** is wrapped in `ApiResponse<T>` (`success`, `message`, `data`, `errors`). Controllers return DTOs from `API/DTOs/Request|Response`, never persistence models, for new code.
- **Tenancy**: everything is scoped by `(UserId, RealmId)`. `CurrentUserMiddleware` populates the scoped **`IRequestContext`** from the JWT; RealmId resolves as `X-Realm-Id` header → `realm_id` claim → first of the `RealmIds` claim. Inject `IRequestContext` — do not read `HttpContext` claims ad hoc. `ICurrentUser` no longer exists.
- **Worker context**: `SyncWorker` registers `SyncContext` as both `ISyncContext` and `IRequestContext`, populated per Service Bus message, so application services shared with the API resolve correctly.
- **Middleware order** (`Infrastructure/QuickBooksApiWebApplicationExtensions.cs`): CorrelationId → ExceptionHandler → RateLimiter → CORS → CurrentUser.
- **Data access**: Dapper only, connections from `ISqlConnectionFactory` (or `ISqlExecutor` for single round-trips) — never `new SqlConnection`. Parameterized SQL only; extract SQL over ~40 lines into a const/`*Queries` class. See `QuickBooksAPI/DataAccessLayer/SQL_CONVENTIONS.md`.
- **DI**: registrations are grouped per bounded context in `Infrastructure/*ServiceCollectionExtensions.cs` (+ `DependencyInjection.Repositories.Core|Analytics.cs`). Add a new feature's registrations to its own extension file, not to a shared root.
- **Config**: typed options via `AddQuickBooksTypedOptions` (validated on start). No new magic strings. Local secrets go in user-secrets (`docs/USER_SECRETS.md`), never appsettings.
- **Full sync steps**: one `IFullSyncEntitySyncStep` per entity in `SyncWorker/Steps/`, ordered in `SyncWorker/Program.cs`. Add an entity by adding a step file, not by editing the orchestrator. Post-sync side effects go through `IFullSyncCompletedSubscriber`.

## Database

**There are no migrations.** Schema lives as hand-run scripts in `Scripts/*.sql` (`Create*.sql`, `Alter*.sql`, `Upsert*.sql` stored procs). Code assumes the tables exist; a missing table is a runtime failure, not a build failure. When adding a table or SP, add the script and note it in the relevant checklist under `tests/docs/` (e.g. `PHASE3_DATABASE_CHECKLIST.md`).

## Frontend conventions

- HTTP lives in `src/api/`: `core.ts` (base URL, token/realm storage, `apiClient`, `ApiError`) plus one `<domain>Api.ts` per domain. `client.ts` is only a barrel — prefer importing `@/api/<domain>Api`. JWT in `sessionStorage`, realm in `localStorage`, `X-Realm-Id` on every request.
- Cross-cutting slices live in `src/features/` (`auth`, `company`, entity slices); routed pages in `src/pages/`; data hooks in `src/hooks/`. Auth/QBO connection belongs to `@/features/auth` — do not create a second auth context.
- Backend base URL comes from `VITE_API_URL` (see `.env.example`); the backend must list the frontend origin in `Cors:AllowedOrigins`.

## Edit-zone policy (`docs/AI_GUIDELINES.md`)

Freely editable: `QuickBooksAPI/Features/**`, `Integrations/**`, `Contracts/**`, `tests/**`, `app/src/features/**`, `app/src/api/**`.

Flag for human review before changing: `QuickBooksAPI/Program.cs`, `SyncWorker/Program.cs`, `Infrastructure/DependencyInjection.cs`, `QuickBooksAPI/Middleware/**`, `DataAccessLayer/Repos/FinancialWarehouseRepository.cs`, `app/src/api/client.ts`, `Dockerfile`, `.github/workflows/**`.

Requires explicit approval: production schema/migration behaviour, breaking public API contract semantics without versioning, new external packages, anything touching secrets/credentials, disabling auth/correlation/security controls, deleting tests.
