<#
.SYNOPSIS
  Validates that QuickBooksAPI appsettings.json contains expected sections (and optionally non-empty secrets).

.PARAMETER Strict
  Fail if Jwt:Key or ConnectionStrings:DefaultConnection are missing or whitespace (for local runs).

.PARAMETER Ci
  Structure-only check: allow empty strings; use in CI where secrets are not injected into appsettings.json.
#>
param(
    [switch] $Strict,
    [switch] $Ci
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$appSettingsPath = Join-Path $repoRoot "QuickBooksAPI\appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    Write-Error "Missing $appSettingsPath"
    exit 1
}

$json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

$requiredSections = @(
    "Logging",
    "ConnectionStrings",
    "Jwt",
    "QuickBooks",
    "Cors",
    "RateLimiting",
    "ServiceBus",
    "HealthChecks"
)

foreach ($name in $requiredSections) {
    if (-not ($json.PSObject.Properties.Name -contains $name)) {
        Write-Error "appsettings.json is missing required section: $name"
        exit 1
    }
}

$nested = @{
    "ConnectionStrings" = @("DefaultConnection")
    "Jwt"               = @("Key", "Issuer", "Audience")
    "QuickBooks"        = @("ClientId", "ClientSecret", "RedirectUri", "RequestURL")
    "ServiceBus"        = @("ConnectionString", "QueueName")
    "HealthChecks"      = @("IncludeDatabase")
}

foreach ($section in $nested.Keys) {
    $obj = $json.$section
    foreach ($key in $nested[$section]) {
        $prop = $obj.PSObject.Properties[$key]
        if ($null -eq $prop) {
            Write-Error "Missing $section`:$key in appsettings.json"
            exit 1
        }
        $val = $prop.Value
        if (-not $Ci -and $Strict) {
            $isEmpty = ($null -eq $val) -or (($val -is [string]) -and [string]::IsNullOrWhiteSpace([string]$val))
            if ($isEmpty -and $section -eq "ConnectionStrings" -and $key -eq "DefaultConnection") {
                Write-Error "Strict: set ConnectionStrings:DefaultConnection (or use user secrets)."
                exit 1
            }
            if ($isEmpty -and $section -eq "Jwt" -and $key -eq "Key") {
                Write-Error "Strict: set Jwt:Key (or use user secrets)."
                exit 1
            }
        }
    }
}

Write-Host "verify-dev-prerequisites: OK ($appSettingsPath)$(if ($Ci) { ' [Ci mode]' } elseif ($Strict) { ' [Strict]' } else { '' })"
exit 0
