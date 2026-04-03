# Local Governance Scripts

## Purpose
These scripts mirror key CI checks locally before commits.

## Scripts
- `run-local-checks.ps1`
  - Runs architecture tests
  - Runs file size governance check (warning mode)
  - Runs frontend lint (unless `-SkipFrontend` is set)

- `install-git-hooks.ps1`
  - Installs a basic `pre-commit` hook in `.git/hooks`

- `install-git-hooks.cmd`
  - Windows-friendly hook installer alternative

## Usage

### Run checks manually
```powershell
powershell -ExecutionPolicy Bypass -File tests/scripts/run-local-checks.ps1
```

### Install hooks (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File tests/scripts/install-git-hooks.ps1
```

### Install hooks (CMD)
```cmd
tests\scripts\install-git-hooks.cmd
```

## Notes
- Branch protection must still be configured in repository settings to enforce CI on PRs.
- **Manual smoke / ops docs:** `tests/docs/` (`FULLSYNC_WORKER_SMOKE.md`, `PHASE3_DATABASE_CHECKLIST.md`, `TRACK_B_API_SMOKE.md`, `SYNCWORKER_TIMERS.md`).
- **File size exclusions:** pass `-ExcludePathSubstrings @("Migrations","Generated")` (example) to skip paths matching substrings.
- **CI parity:** `governance` enforces `-FailOnViolation` on `QuickBooksAPI`, `SyncWorker`, `tests`, and `QuickBooksAPI Frontend/app/src` (with `components\ui` excluded for shadcn). Backend job runs `dotnet format QuickBooksAPI.sln --verify-no-changes` on the full solution.


