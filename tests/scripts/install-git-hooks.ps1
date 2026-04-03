$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "."
$hooksDir = Join-Path $repoRoot ".git\hooks"

if (-not (Test-Path $hooksDir)) {
    throw "Git hooks directory not found. Ensure this is a git repository."
}

$preCommitPath = Join-Path $hooksDir "pre-commit"
$script = @'
#!/usr/bin/env bash
set -euo pipefail

echo "Running pre-commit local checks..."
powershell -ExecutionPolicy Bypass -File "tests/scripts/run-local-checks.ps1"
'@

Set-Content -Path $preCommitPath -Value $script -NoNewline

Write-Host "Installed pre-commit hook at $preCommitPath"
Write-Host "Note: If running on Windows without bash, create a matching pre-commit.ps1 hook as needed."

