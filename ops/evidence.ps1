param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$OutDir = "evidence"
)

$ErrorActionPreference = "Continue"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$timestamp = Get-Date -Format "yyyyMMdd-HHmm"
$bundleDir = Join-Path $rootDir (Join-Path $OutDir $timestamp)
$testsDir = Join-Path $bundleDir "tests"
$failures = New-Object System.Collections.Generic.List[string]
$stepResults = @{}

New-Item -ItemType Directory -Force -Path $bundleDir, $testsDir | Out-Null

Write-Host "[1/6] Swagger export"
try {
    $swaggerCandidates = @(
        "$ApiBaseUrl/swagger/v1/swagger.json",
        "$ApiBaseUrl//swagger/v1/swagger.json"
    )
    $swaggerSuccess = $false
    foreach ($candidate in $swaggerCandidates) {
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            try {
                Invoke-WebRequest -Uri $candidate -OutFile (Join-Path $bundleDir "swagger.json") -TimeoutSec 20
                $swaggerSuccess = $true
                break
            } catch {
                if ($attempt -lt 3) { Start-Sleep -Seconds 1 }
            }
        }
        if ($swaggerSuccess) { break }
    }

    if ($swaggerSuccess) {
        $stepResults["swagger"] = "OK"
    } else {
        "WARN: Swagger indirilemedi ($ApiBaseUrl). Devam ediliyor." | Out-File -FilePath (Join-Path $bundleDir "swagger.warn.txt") -Encoding utf8
        $failures.Add("swagger")
        $stepResults["swagger"] = "FAIL"
    }
} catch {
    "WARN: Swagger adimi beklenmeyen hata verdi: $($_.Exception.Message)" | Out-File -FilePath (Join-Path $bundleDir "swagger.warn.txt") -Encoding utf8
    $failures.Add("swagger")
    $stepResults["swagger"] = "FAIL"
}

Write-Host "[2/6] Test reports"
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    try {
        $unitProj = Join-Path $rootDir "backend/tests/IkOtomasyon.Api.Tests/IkOtomasyon.Api.Tests.csproj"
        $integrationProj = Join-Path $rootDir "backend/tests/IntegrationTests/IkOtomasyon.Api.IntegrationTests.csproj"
        $testsFailed = $false
        if ((Test-Path $unitProj) -and (Test-Path $integrationProj)) {
            $unitDir = Join-Path $testsDir "unit"
            $integrationDir = Join-Path $testsDir "integration"
            New-Item -ItemType Directory -Force -Path $unitDir, $integrationDir | Out-Null

            dotnet test $unitProj -c Release --no-build --logger "trx;LogFileName=unit.trx" --results-directory $unitDir
            if ($LASTEXITCODE -ne 0) { $testsFailed = $true }

            dotnet test $integrationProj -c Release --no-build --logger "trx;LogFileName=integration.trx" --results-directory $integrationDir
            if ($LASTEXITCODE -ne 0) { $testsFailed = $true }
        } else {
            $ts = Get-Date -Format "yyyyMMdd-HHmmss"
            dotnet test -c Release --no-build --logger "trx;LogFileName=tests-$ts.trx" --results-directory $testsDir
            if ($LASTEXITCODE -ne 0) { $testsFailed = $true }
        }

        if ($testsFailed) { $failures.Add("tests"); $stepResults["tests"] = "FAIL" } else { $stepResults["tests"] = "OK" }
    } catch {
        $failures.Add("tests")
        $stepResults["tests"] = "FAIL"
    }
} else {
    "dotnet bulunamadi, test raporu alinmadi" | Out-File -FilePath (Join-Path $testsDir "warning.txt") -Encoding utf8
    $failures.Add("tests")
    $stepResults["tests"] = "FAIL"
}

Write-Host "[3/6] Migrations list"
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    try {
        Push-Location $rootDir
        dotnet tool restore *>&1 | Out-Null
        Pop-Location
        Push-Location (Join-Path $rootDir "backend")
        $migrationsOutput = dotnet dotnet-ef migrations list *>&1
        $migrationsPath = Join-Path $bundleDir "migrations.txt"
        $migrationsOutput | Out-File -FilePath $migrationsPath -Encoding utf8
        $hasDbAccessIssue = ($migrationsOutput | Out-String) -match "Pending status not shown|error occurred while accessing the database|Failed to connect"
        if ($LASTEXITCODE -ne 0 -or $hasDbAccessIssue) {
            if (Get-Command docker -ErrorAction SilentlyContinue) {
                Pop-Location
                $composeFile = Join-Path $rootDir "docker-compose.prod.yml"
                $envFile = Join-Path $rootDir ".env.prod"
                if ((Test-Path $composeFile) -and (Test-Path $envFile)) {
                    "---- fallback: docker compose migrate check ----" | Out-File -FilePath $migrationsPath -Append -Encoding utf8
                    docker compose -f $composeFile --env-file $envFile run --rm migrate *>&1 | Out-File -FilePath $migrationsPath -Append -Encoding utf8
                    if ($LASTEXITCODE -eq 0) {
                        $stepResults["migrations"] = "OK"
                    } else {
                        $failures.Add("migrations")
                        $stepResults["migrations"] = "FAIL"
                    }
                } else {
                    $failures.Add("migrations")
                    $stepResults["migrations"] = "FAIL"
                }
            } else {
                $failures.Add("migrations")
                $stepResults["migrations"] = "FAIL"
            }
        } else {
            $stepResults["migrations"] = "OK"
        }
    } catch {
        $_ | Out-File -FilePath (Join-Path $bundleDir "migrations.txt") -Encoding utf8
        $failures.Add("migrations")
        $stepResults["migrations"] = "FAIL"
    } finally {
        if ((Get-Location).Path -ne $rootDir) { Pop-Location }
    }
} else {
    "dotnet bulunamadi" | Out-File -FilePath (Join-Path $bundleDir "migrations.txt") -Encoding utf8
    $failures.Add("migrations")
    $stepResults["migrations"] = "FAIL"
}

Write-Host "[4/6] Vulnerability scan"
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    try {
        Push-Location (Join-Path $rootDir "backend")
        dotnet list package --vulnerable --include-transitive *>&1 | Out-File -FilePath (Join-Path $bundleDir "vuln.txt") -Encoding utf8
        if ($LASTEXITCODE -ne 0) { $failures.Add("vuln-scan"); $stepResults["vuln-scan"] = "FAIL" } else { $stepResults["vuln-scan"] = "OK" }
    } catch {
        $_ | Out-File -FilePath (Join-Path $bundleDir "vuln.txt") -Encoding utf8
        $failures.Add("vuln-scan")
        $stepResults["vuln-scan"] = "FAIL"
    } finally {
        Pop-Location
    }
} else {
    "dotnet bulunamadi" | Out-File -FilePath (Join-Path $bundleDir "vuln.txt") -Encoding utf8
    $failures.Add("vuln-scan")
    $stepResults["vuln-scan"] = "FAIL"
}

Write-Host "[5/6] CI summary"
if (Test-Path (Join-Path $rootDir ".github/workflows/ci.yml")) {
    @(
        "CI workflow file: .github/workflows/ci.yml"
        "Expected jobs: build_test, migrations_drift, security_vuln, frontend build"
        "Run URL template: <repo>/actions/workflows/ci.yml"
    ) | Out-File -FilePath (Join-Path $bundleDir "ci-summary.txt") -Encoding utf8
    $stepResults["ci-summary"] = "OK"
} else {
    "ci.yml bulunamadi" | Out-File -FilePath (Join-Path $bundleDir "ci-summary.txt") -Encoding utf8
    $failures.Add("ci-summary")
    $stepResults["ci-summary"] = "FAIL"
}

Write-Host "[6/6] Index"
@(
    "Evidence bundle generated at: $timestamp"
    "API base: $ApiBaseUrl"
    "Files:"
    "- swagger.json"
    "- migrations.txt"
    "- vuln.txt"
    "- ci-summary.txt"
    "- tests/*.trx"
) | Out-File -FilePath (Join-Path $bundleDir "README.txt") -Encoding utf8

if ($failures.Count -gt 0) {
    "Failures: $($failures -join ', ')" | Out-File -FilePath (Join-Path $bundleDir "summary.txt") -Encoding utf8
    Write-Host "Completed with warnings. See $bundleDir\\summary.txt"
} else {
    "All steps succeeded." | Out-File -FilePath (Join-Path $bundleDir "summary.txt") -Encoding utf8
}

@(
    "swagger=$($stepResults['swagger'])"
    "tests=$($stepResults['tests'])"
    "migrations=$($stepResults['migrations'])"
    "vuln-scan=$($stepResults['vuln-scan'])"
    "ci-summary=$($stepResults['ci-summary'])"
) | Out-File -FilePath (Join-Path $bundleDir "step-status.txt") -Encoding utf8

Write-Host "Done: $bundleDir"
