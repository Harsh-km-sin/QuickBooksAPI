# Baseline Architecture Metrics (Phase 0)

Date: 2026-03-26

## Objective
Capture a lightweight baseline before structural refactoring starts.

## Hotspot Files by Size (selected)
- `QuickBooksAPI/Services/CustomerService.cs` - 583 lines
- `QuickBooksAPI/Services/VendorService.cs` - 422 lines
- `QuickBooksAPI/Services/AuthServices.cs` - 387 lines
- `QuickBooksAPI/Services/InvoiceService.cs` - 372 lines
- `QuickBooksAPI/DataAccessLayer/Repos/FinancialWarehouseRepository.cs` - 344 lines
- `QuickBooksAPI Frontend/app/src/api/client.ts` - 407 lines
- `QuickBooksAPI Frontend/app/src/types/index.ts` - 582 lines
- `QuickBooksAPI Frontend/app/src/components/ui/sidebar.tsx` - 672 lines

## Existing Guardrails Added in Phase 0
- Architecture tests project: `tests/ArchitectureTests`
- CI workflow: `.github/workflows/ci.yml`
- File size governance script (warning mode): `tests/scripts/check-file-size.ps1`
- AI policy and edit boundaries: `AI_GUIDELINES.md`

## Architecture Test Baseline
- Added dependency rules to prevent direct Controller coupling from Services/Repositories.
- Current local result: **3/3 tests passing**.

## File Size Governance Baseline
- Script currently reports oversized files in warning mode.
- Largest hotspots align with known refactor targets:
  - backend service god classes
  - monolithic frontend API/types/ui files

## Smoke Validation Checklist (Current)
- [x] Solution builds (via architecture test run build chain)
- [x] Architecture tests pass
- [ ] Full backend API smoke endpoints run
- [ ] Frontend lint/build run locally
- [ ] Worker trigger end-to-end smoke test

## Notes
- Existing nullable warnings are pre-existing and not introduced by Phase 0 changes.
- File size enforcement is intentionally warning-only during Phase 0.

