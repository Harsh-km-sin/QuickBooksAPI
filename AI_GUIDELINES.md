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

## Mandatory Validation Before Merge
- Build passes for backend and worker.
- Frontend lint/build passes.
- Architecture tests pass.
- Secret scan passes.
- Dependency vulnerability checks pass.

