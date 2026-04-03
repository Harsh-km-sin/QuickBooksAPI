param(
    [string]$Root = ".",
    [int]$MaxLines = 300,
    [switch]$FailOnViolation,
    # Relative path substrings to skip (e.g. "Generated", "Migrations"). Case-insensitive.
    [string[]]$ExcludePathSubstrings = @()
)

$extensions = @("*.cs", "*.ts", "*.tsx")
$excludePatterns = @(
    "*\bin\*",
    "*\obj\*",
    "*\dist\*",
    "*\node_modules\*"
)

$violations = @()

foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $Root -Recurse -File -Filter $ext
    foreach ($file in $files) {
        $full = $file.FullName
        $skip = $false
        foreach ($pattern in $excludePatterns) {
            if ($full -like $pattern) {
                $skip = $true
                break
            }
        }

        if ($skip) { continue }

        foreach ($sub in $ExcludePathSubstrings) {
            if ($sub -and ($full -like "*$sub*")) {
                $skip = $true
                break
            }
        }
        if ($skip) { continue }

        $lineCount = (Get-Content -Path $full | Measure-Object -Line).Lines
        if ($lineCount -gt $MaxLines) {
            $violations += [PSCustomObject]@{
                File = $full
                Lines = $lineCount
            }
        }
    }
}

if ($violations.Count -eq 0) {
    Write-Host "File size check passed. No files exceed $MaxLines lines."
    exit 0
}

if ($FailOnViolation) {
    Write-Host "File size check failed. Files exceeding $MaxLines lines:"
} else {
    Write-Host "File size check warning. Files exceeding $MaxLines lines:"
}

$violations | Sort-Object Lines -Descending | ForEach-Object {
    Write-Host "$($_.Lines) - $($_.File)"
}

if ($FailOnViolation) {
    exit 1
}

exit 0

