# Prints paths to operational parity / smoke docs (run after dotnet test in CI logs for discoverability).
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Write-Host "Operational parity docs (repo root: $root)"
Write-Host "  - tests/docs/PHASE3_DATABASE_CHECKLIST.md"
Write-Host "  - tests/docs/TRACK_B_API_SMOKE.md"
Write-Host "  - tests/docs/FULLSYNC_WORKER_SMOKE.md"
Write-Host ""
& (Join-Path $PSScriptRoot "verify-dev-prerequisites.ps1") -Ci
