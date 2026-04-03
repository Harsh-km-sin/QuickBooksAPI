# Phase 3 Implementation Tracker (two parallel tracks)

## Why two “Phase 3”s
- **Track A — Auth engineering** matches `AI_READINESS_IMPROVEMENT_ROADMAP.md`: shrink `AuthServices`, clearer boundaries, tests. Goal: safe, reviewable changes to login/OAuth/tokens.
- **Track B — Product Phase 3** matches `README.md`: forecasting, CFO assistant, close & data quality, consolidation. Goal: ship CFO/analytics capabilities (see README for APIs, UI, scripts).

Assign **owners per track** if more than one person; if solo, **sequence** (e.g. Auth first if it blocks everything, or product first if demos/revenue matter more).

## How to maintain this file
- Check `[ ]` → `[x]` when work ships in the same PR when possible.
- Re-run **Verified** after meaningful backend or worker changes.
- Keep **Track A** and **Track B** checklists honest: don’t mark product items done until API + worker + DB scripts + UI match README intent.

---

## Master checklist (sprint board)

### Track A — Auth decomposition (AI readiness)
- [x] Extract interfaces + implementations from `AuthServices` (see §3A below)
- [x] Keep `IAuthService` façade + register slices in API + `SyncWorker`
- [x] Unit tests: `tests/QuickBooksAPI.UnitTests/Auth/*` (validator, registration, login, QBO callback/disconnect, token lifecycle, connected companies, façade delegation); run `dotnet test QuickBooksAPI.sln`

### Track B — Product Phase 3 (`README.md`)
- [x] **Forecasting (in repo):** `IForecastService`, `AnalyticsController` forecast routes, `Forecast` UI, `analyticsApi` — [ ] confirm tables + smoke ([`tests/docs/TRACK_B_API_SMOKE.md`](tests/docs/TRACK_B_API_SMOKE.md))
- [x] **CFO Assistant (in repo):** `CfoAssistantController`, `ICfoAssistantService`, UI — [ ] confirm Azure OpenAI settings in target env if using LLM
- [x] **Close & data quality (in repo):** `CloseIssuesFunction`, close-issues APIs, `CloseAssistant`, dashboard — [ ] confirm `close_issues` table + deployed timer
- [x] **Consolidation (in repo):** `ConsolidationFunction`, entities + consolidated-pnl APIs, dashboard toggle — [ ] confirm consolidation tables + `dim_entity` seed ([`Scripts/SeedDimEntity_Example.sql`](Scripts/SeedDimEntity_Example.sql))
- [ ] **Environments:** run/verify SQL scripts per [`tests/docs/PHASE3_DATABASE_CHECKLIST.md`](tests/docs/PHASE3_DATABASE_CHECKLIST.md) in dev/staging/prod

### Carry-over from Phase 2 (confidence, not “Phase 3”)
- [x] Full sync E2E **runbook:** [`tests/docs/FULLSYNC_WORKER_SMOKE.md`](tests/docs/FULLSYNC_WORKER_SMOKE.md)
- [ ] **Execute** runbook in a real environment (Service Bus + SQL + QBO) and mark Phase 2 §2.2 complete

---

## Verified (refresh after batches)
- [x] `dotnet build QuickBooksAPI.sln` (0 errors)
- [x] `dotnet build SyncWorker/SyncWorker.csproj` (0 errors)
- [x] `dotnet test QuickBooksAPI.sln -c Release` (pass: architecture + unit tests)
- [x] Unit tests: `tests/QuickBooksAPI.UnitTests/Analytics/ForecastServiceTests.cs` (forecast compute + null path)
- [ ] Track B: run [`tests/docs/TRACK_B_API_SMOKE.md`](tests/docs/TRACK_B_API_SMOKE.md) against a deployed API

---

## §3A — Auth split (detail)

Goal: decompose auth into coherent modules; `AuthServices` remains a thin `IAuthService` façade in `Services/AuthServices.cs`. Implementations live in `Services/Auth/`.

- [x] `IUserRegistrationService` → `UserRegistrationService`
- [x] `IUserLoginService` → `UserLoginService` (JWT issuance private to this type)
- [x] `IQboConnectionService` → `QboConnectionService` (OAuth URL, callback, disconnect)
- [x] `IQboTokenLifecycleService` → `QboTokenLifecycleService` (QBO access-token expiry + refresh; name avoids confusion with app JWT)
- [x] `IConnectedCompanyQueryService` → `ConnectedCompanyQueryService`
- [x] Shared sign-up field rules: `IUserSignUpValidator` + `UserSignUpRequestValidator` (singleton); `UserRegistrationService` trims then validates
- [x] Token refresh/expiry in `QboTokenLifecycleService` only
- [x] `QuickBooksAPI/Program.cs` + `SyncWorker/Program.cs` register all slices + façade

---

## §3B — Product Phase 3 (detail, from README)

Use `README.md` **Phase 3** as source of truth for behavior names and routes. Rough checklist:

### Forecasting
- [ ] Tables `forecast_scenarios`, `forecast_results` (**apply scripts** in each environment — [`PHASE3_DATABASE_CHECKLIST.md`](tests/docs/PHASE3_DATABASE_CHECKLIST.md))
- [x] `IForecastService` create/compute + fetch (`ForecastService`, `AnalyticsController`)
- [x] `POST /api/Analytics/forecast`, `GET /api/Analytics/forecast/{id}` (case-insensitive routing)
- [x] `Forecast` UI page + `analyticsApi` client

### CFO Assistant
- [x] `POST /api/cfo-assistant/ask` + `ICfoAssistantService`
- [x] Config: `AzureOpenAiOptions` / `AzureOpenAI:*` (see `README.md`; optional LLM)
- [x] CFO Assistant UI (`/cfo-assistant`)

### Close & data quality
- [ ] Table `close_issues` (**script** in each environment)
- [x] `CloseIssuesFunction` timer — schedule vs README: [`tests/docs/SYNCWORKER_TIMERS.md`](tests/docs/SYNCWORKER_TIMERS.md)
- [x] `GET /api/Analytics/close-issues`, `POST .../close-issues/{id}/resolve`
- [x] Dashboard + `CloseAssistant` page

### Consolidation
- [ ] Tables `dim_entity`, `fact_consolidated_pnl` (**script** in each environment)
- [x] `ConsolidationFunction` (monthly timer — see [`SYNCWORKER_TIMERS.md`](tests/docs/SYNCWORKER_TIMERS.md))
- [x] `GET /api/Analytics/entities`, `GET /api/Analytics/consolidated-pnl`
- [x] Dashboard consolidated toggle + entity selector + chart

### Data hygiene
- [ ] Seed/maintain `dim_entity` (example: [`Scripts/SeedDimEntity_Example.sql`](Scripts/SeedDimEntity_Example.sql))
