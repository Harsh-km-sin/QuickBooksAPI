# Exports Swagger JSON from a locally running QuickBooksAPI instance.
# Requires a valid appsettings profile so the host can start (see appsettings.OpenApiExport.json + ASPNETCORE_ENVIRONMENT).
param(
    [int]$Port = 17736,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$apiProject = Join-Path $RepoRoot "QuickBooksAPI\QuickBooksAPI.csproj"
$outFile = Join-Path $RepoRoot "QuickBooksAPI Frontend\app\openapi\openapi-v1.json"
$url = "http://127.0.0.1:$Port/swagger/v1/swagger.json"

New-Item -ItemType Directory -Force -Path (Split-Path $outFile) | Out-Null

dotnet build $apiProject -c Debug --nologo -v minimal | Out-Null

$env:ASPNETCORE_ENVIRONMENT = "OpenApiExport"
$proc = Start-Process -FilePath "dotnet" -ArgumentList @(
    "run",
    "--no-build",
    "-c", "Debug",
    "--project", $apiProject,
    "--no-launch-profile",
    "--urls", "http://127.0.0.1:$Port"
) -PassThru -WindowStyle Hidden

try {
    $ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if (-not $ready) { throw "Swagger did not become available on port $Port within timeout." }

    Invoke-WebRequest -Uri $url -UseBasicParsing | Select-Object -ExpandProperty Content | Set-Content -Path $outFile -Encoding utf8
    Write-Host "Wrote $outFile"
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
