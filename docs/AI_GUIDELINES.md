# AI Agent Coding Guidelines

This document defines what AI agents can and cannot change in this repository during the architecture migration.

## Safe Edit Zones (AI MAY modify)
- `Features/**`
- `Integrations/**`
- `Contracts/**`
- `tests/**`
- `QuickBooksAPI Frontend/app/src/features/**`
- `QuickBooksAPI Frontend/app/src/api/**`
- Existing service/repository/controller files only when task-scoped and tested

## High-Risk Shared Files (Human Review Required)
- `QuickBooksAPI/Program.cs`
- `SyncWorker/Program.cs`
- `QuickBooksAPI/Infrastructure/DependencyInjection.cs`
- `QuickBooksAPI/Middleware/**`
- `QuickBooksAPI/DataAccessLayer/Repos/FinancialWarehouseRepository.cs`
- `QuickBooksAPI Frontend/app/src/api/client.ts`
- `Dockerfile`
- `.github/workflows/**`

## Prohibited Without Explicit Approval
- Modifying DB schema or migration behavior in production
- Changing public API contract semantics without versioning
- Introducing new external dependencies/packages
- Adding or changing secrets, credentials, connection strings
- Disabling security controls, auth checks, or correlation tracing
- Deleting tests or reducing test coverage gates

## Required Development Standards
- Keep classes focused (single responsibility).
- Prefer interfaces for infrastructure/external dependencies.
- No new static mutable global state.
- Use typed options for configuration (no new magic strings).
- Add or update tests for behavior changes.
- Keep PRs small and feature-scoped.

## Dependency Direction (Target)
- `API -> Application`
- `Application -> Domain + Integrations.Abstractions`
- `Infrastructure -> Application.Contracts + Domain`
- `Integrations.Providers -> Integrations.Abstractions`
- `Worker -> Application + Contracts`
- `Tests -> Any production code`

### Feature layer vs data access (enforced)
- Types under `QuickBooksAPI.Features` **must not** depend on the namespace `QuickBooksAPI.DataAccessLayer.Repos` (see `tests/ArchitectureTests`). Put repository **interfaces** in `QuickBooksAPI/Application/Interfaces/`; implementations stay under `DataAccessLayer/Repos/`. Row/DTO shapes used across layers belong in `DataAccessLayer/Models` (or application DTOs), not nested under a single repo file.

## Mandatory Validation Before Merge
- Build passes for backend and worker.
- Frontend lint/build passes.
- Architecture tests pass.
- Secret scan passes.
- Dependency vulnerability checks pass.

## Definition of Done — AI readiness targets

Targets: **Single-AI ~9/10**, **Multi-AI ~8/10** (see `AI_READINESS_IMPROVEMENT_ROADMAP.md`). Use this checklist when closing architecture work.

### Single-AI 9 (one agent, minimal cross-file context)

- **Application contracts:** New or migrated code in `Application/Interfaces` does not reference `QuickBooksAPI.DataAccessLayer.Models`; list/read APIs use `Application/Dtos` (or domain types not tied to persistence). Legacy interfaces may remain until migrated; NetArch allowlist tracks exceptions.
- **File size:** Owned application code respects CI limits (`tests/scripts/check-file-size.ps1`, typically 300 lines); split before adding surface area.
- **Feature pairing:** Each major capability has a clear pair: backend `Features/<Name>/` (or module) + frontend `api/<name>Api.ts` (and optional `features/<name>/`).
- **API contract visibility:** OpenAPI document is produced in CI (artifact) so types and routes are discoverable without reading the whole tree; optional generated TS types in `app/src/api/generated/`.

### Multi-AI 8 (parallel agents, low merge conflict)

- **Ownership:** Maintain [`.github/CODEOWNERS`](.github/CODEOWNERS) (replace placeholder team handles with real `@org/team` or `@username` values) **and** use the feature/API pairing table in [`QuickBooksAPI/Features/README.md`](QuickBooksAPI/Features/README.md) + [`QuickBooksAPI Frontend/app/README.md`](QuickBooksAPI%20Frontend/app/README.md) so two agents rarely edit the same file.
- **No cross-feature internals:** Features communicate via application interfaces or contracts, not by reaching into another feature’s private types.
- **Sync and DI:** Full-sync steps are separate types/files (`ISyncStep` / per-entity step); repository and service registration is grouped by bounded context so a typical PR touches one extension file, not the global `DependencyInjection` root only.

### Smoke and environment parity (confidence, not a code metric)

- Runbooks executed and recorded where applicable: `tests/docs/TRACK_B_API_SMOKE.md`, `tests/docs/FULLSYNC_WORKER_SMOKE.md`, `tests/scripts/verify-dev-prerequisites.ps1`, `tests/docs/PHASE3_DATABASE_CHECKLIST.md` (see `PHASE3_IMPLEMENTATION_TRACKER.md`).

