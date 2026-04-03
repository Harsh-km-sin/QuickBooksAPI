# Phase 0 Implementation Tracker

## Scope
Phase 0 focuses on scaffolding and governance guardrails:
- folder scaffolding
- AI change policy
- architecture test harness
- CI baseline checks
- file size governance script

## Completed
- [x] Created scaffold folders:
  - [x] `Features/`
  - [x] `Integrations/`
  - [x] `Contracts/`
  - [x] `tests/`
- [x] Added `AI_GUIDELINES.md`
- [x] Created architecture tests project: `tests/ArchitectureTests`
- [x] Added initial dependency architecture tests (`DependencyRulesTests`)
- [x] Added architecture tests project to solution
- [x] Added CI workflow: `.github/workflows/ci.yml`
- [x] Added file size governance script: `tests/scripts/check-file-size.ps1`
- [x] Added baseline metrics report: `BASELINE_ARCHITECTURE_METRICS.md`
- [x] Added PR checklist template: `.github/pull_request_template.md`
- [x] Added local check runner: `tests/scripts/run-local-checks.ps1`
- [x] Added local git hook installers:
  - [x] `tests/scripts/install-git-hooks.ps1`
  - [x] `tests/scripts/install-git-hooks.cmd`
- [x] Added local script usage docs: `tests/scripts/README.md`

## Remaining for Full Phase 0 Completion
- [ ] Add branch protection rules to enforce CI checks (GitHub settings)
- [ ] Tune file-size script exclusions if needed: `tests/scripts/check-file-size.ps1` supports `-ExcludePathSubstrings @("YourPath")` (CI can pass the same parameter in `.github/workflows/ci.yml`)

## Completed (follow-up)
- [x] `check-file-size.ps1`: optional `-ExcludePathSubstrings` for noisy generated or vendor paths

