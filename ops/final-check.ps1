$ErrorActionPreference = "Continue"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$warn = $false

Write-Host "[1/5] Port check"
function Check-Url($url, $name) {
    try {
        $res = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5
        if ($res.StatusCode -in 200,301,302) {
            Write-Host "OK: $name ($url)"
            return
        }
    } catch {}
    Write-Host "WARN: $name not ready ($url)"
    $script:warn = $true
}
Check-Url "http://localhost:3000" "web"
Check-Url "http://localhost:8080/swagger" "api"

Write-Host "[2/5] Required files"
$required = @("VERSION","CHANGELOG.md","docs/acceptance-signoff.md","docs/risk-register.md","docs/demo-10min-cheatsheet.md")
foreach ($f in $required) {
    if (Test-Path (Join-Path $rootDir $f)) { Write-Host "OK: $f" } else { Write-Host "WARN: missing $f"; $warn = $true }
}

Write-Host "[3/5] Submission artifacts"
$zip = Get-ChildItem -Path $rootDir -Filter "ik-otomasyon-submission-v*.zip" -ErrorAction SilentlyContinue
if ($zip) { Write-Host "OK: submission zip exists" } else { Write-Host "WARN: submission zip missing"; $warn = $true }
if (Test-Path (Join-Path $rootDir "submission/MANIFEST.txt")) { Write-Host "OK: submission/MANIFEST.txt exists" } else { Write-Host "WARN: submission/MANIFEST.txt missing"; $warn = $true }

Write-Host "[4/5] Evidence latest"
$evidenceRoot = Join-Path $rootDir "evidence"
if (Test-Path $evidenceRoot) {
    $dirs = Get-ChildItem -Path $evidenceRoot -Directory | Sort-Object Name
    if ($dirs.Count -gt 0) {
        Write-Host "OK: evidence dir -> $($dirs[-1].FullName)"
    } else {
        Write-Host "WARN: no evidence run found"
        $warn = $true
    }
} else {
    Write-Host "WARN: no evidence folder"
    $warn = $true
}

Write-Host "[5/5] Secret/PII scan"
$pk = rg -n "BEGIN PRIVATE KEY|BEGIN RSA PRIVATE KEY" $rootDir --glob "!backend/bin/**" --glob "!backend/obj/**" --glob "!frontend/node_modules/**" --glob "!evidence/**" --glob "!submission/**" --glob "!ops/**"
if ($LASTEXITCODE -eq 0 -and $pk) { Write-Host "WARN: private key pattern found"; $warn = $true } else { Write-Host "OK: no private key pattern" }

$jwt = rg -n "JWT_SECRET=" $rootDir --glob "!.env*" --glob "!*.example" --glob "!docs/**" --glob "!submission/**" --glob "!evidence/**" --glob "!ops/**"
if ($LASTEXITCODE -eq 0 -and $jwt) { Write-Host "WARN: JWT_SECRET assignment found in tracked files"; $warn = $true } else { Write-Host "OK: no JWT_SECRET assignment in tracked source/docs" }

$pii = rg -n "Demo Candidate|demo@local\\.test" (Join-Path $rootDir "sample-data")
if ($LASTEXITCODE -eq 0 -and $pii) { Write-Host "OK: sample-data appears anonymized" } else { Write-Host "WARN: sample-data markers not found"; $warn = $true }

if ($warn) { Write-Host "FINAL CHECK: WARN"; exit 0 }
Write-Host "FINAL CHECK: OK"
