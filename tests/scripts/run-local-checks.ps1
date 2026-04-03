param(
    [switch]$SkipFrontend
)

$ErrorActionPreference = "Stop"

Write-Host "Running local checks..."

Write-Host "1) Architecture tests"
dotnet test "tests/ArchitectureTests/ArchitectureTests.csproj"

Write-Host "2) File size — backend/worker/tests (strict)"
& "tests/scripts/check-file-size.ps1" -Root "QuickBooksAPI" -MaxLines 300 -FailOnViolation
& "tests/scripts/check-file-size.ps1" -Root "SyncWorker" -MaxLines 300 -FailOnViolation
& "tests/scripts/check-file-size.ps1" -Root "tests" -MaxLines 300 -FailOnViolation

Write-Host "2b) File size — full tree (warning)"
& "tests/scripts/check-file-size.ps1" -Root "." -MaxLines 300

Write-Host "2c) dotnet format (full solution, verify)"
dotnet format QuickBooksAPI.sln --verify-no-changes

if (-not $SkipFrontend) {
    Write-Host "3) Frontend lint"
    Push-Location "QuickBooksAPI Frontend/app"
    try {
        npm run lint
    }
    finally {
        Pop-Location
    }
    Write-Host "3b) File size — frontend src (strict; components/ui excluded)"
    & "$PSScriptRoot/check-file-size.ps1" -Root "QuickBooksAPI Frontend/app/src" -MaxLines 300 -FailOnViolation -ExcludePathSubstrings @("components\ui")
}

Write-Host "Local checks complete."

