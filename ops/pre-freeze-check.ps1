$ErrorActionPreference = "Continue"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$warn = $false

Write-Host "[1/4] Secret pattern scan"
$secretHits = rg -n "BEGIN PRIVATE KEY|BEGIN RSA PRIVATE KEY|AKIA[0-9A-Z]{16}" $rootDir --glob "!backend/bin/**" --glob "!backend/obj/**" --glob "!frontend/node_modules/**" --glob "!evidence/**" --glob "!ops/pre-freeze-check.sh" --glob "!ops/pre-freeze-check.ps1"
if ($LASTEXITCODE -eq 0 -and $secretHits) {
    $warn = $true
    Write-Host "WARN: Potential secret material detected."
    $secretHits
} else {
    Write-Host "OK: No private key / obvious token patterns found."
}

Write-Host "[2/4] .env tracked check"
if (Test-Path (Join-Path $rootDir ".env.prod")) {
    $warn = $true
    Write-Host "WARN: .env.prod file exists locally. Ensure it is not committed."
} else {
    Write-Host "OK: .env.prod not present in repo root."
}

Write-Host "[3/4] Sample-data PII sanity"
$piiHits = rg -n "Demo Candidate|demo@local\\.test" (Join-Path $rootDir "sample-data")
if ($LASTEXITCODE -eq 0 -and $piiHits) {
    Write-Host "OK: sample-data contains demo identity markers."
} else {
    $warn = $true
    Write-Host "WARN: sample-data does not contain expected demo identity markers."
}

Write-Host "[4/4] Required metadata files"
$required = @(
    "VERSION",
    "CHANGELOG.md",
    "docs/acceptance-signoff.md",
    "docs/risk-register.md",
    "docs/freeze.md"
)
foreach ($file in $required) {
    if (!(Test-Path (Join-Path $rootDir $file))) {
        $warn = $true
        Write-Host "WARN: missing $file"
    }
}

if ($warn) {
    Write-Host "Pre-freeze check: WARNINGS_FOUND"
    exit 0
}

Write-Host "Pre-freeze check: PASS"
