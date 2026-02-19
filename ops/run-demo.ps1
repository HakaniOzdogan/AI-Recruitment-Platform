param(
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.prod",
    [string]$ApiUrl = "http://localhost:8080",
    [string]$WebUrl = "http://localhost:3000",
    [int]$TimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Wait-HttpOk {
    param(
        [string]$Url,
        [string]$Name
    )
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            $codeRaw = & curl.exe -s -o NUL -w "%{http_code}" $Url
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($codeRaw) -and [int]$codeRaw -eq 200) {
                Write-Host "Hazir: $Name ($Url)"
                return $true
            }
        } catch {}
        Start-Sleep -Seconds 3
        $elapsed += 3
    }
    throw "Timeout: $Name hazir degil ($Url)"
}

Write-Host "[0/7] Preconditions"
docker --version | Out-Null
docker compose version | Out-Null
try { dotnet --version | Out-Null } catch { Write-Host "Not: dotnet bulunamadi (compose icin zorunlu degil)." }

Set-Location $rootDir

Write-Host "[1/7] Env hazirligi"
if (!(Test-Path $EnvFile)) {
    Copy-Item ".env.prod.example" $EnvFile
    Write-Host "$EnvFile olusturuldu (.env.prod.example kopyasi)."
}

$envContent = Get-Content $EnvFile -Raw
if ($envContent -match "JWT_SECRET=(.*)") {
    $jwtSecret = $Matches[1].Trim()
    if ([string]::IsNullOrWhiteSpace($jwtSecret) -or $jwtSecret -eq "change_me_minimum_32_chars_secret_value") {
        $bytes = New-Object byte[] 48
        [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
        $generated = [Convert]::ToBase64String($bytes)
        $escaped = [Regex]::Escape($jwtSecret)
        $envContent = $envContent -replace "JWT_SECRET=$escaped", "JWT_SECRET=$generated"
        Set-Content -Path $EnvFile -Value $envContent -Encoding utf8
        $jwtSecret = $generated
        Write-Host "JWT_SECRET otomatik uretildi ve $EnvFile dosyasina yazildi."
    }

    if ([string]::IsNullOrWhiteSpace($jwtSecret) -or $jwtSecret.Length -lt 32 -or $jwtSecret -eq "change_me_minimum_32_chars_secret_value") {
        throw "JWT_SECRET gecersiz. $EnvFile icinde en az 32 karakter guclu bir deger olmalidir."
    }
} else {
    throw "JWT_SECRET satiri bulunamadi: $EnvFile"
}
$env:JWT_SECRET = $jwtSecret

$smokeEmail = $env:SMOKE_TEST_EMAIL
$smokePassword = $env:SMOKE_TEST_PASSWORD

Write-Host "[2/7] Compose up (db -> migrate -> api+web)"
if ([string]::IsNullOrWhiteSpace($env:JWT_SECRET)) {
    throw "JWT_SECRET env degiskeni yuklenemedi. $EnvFile dosyasini kontrol edin."
}
try {
    docker compose -f $ComposeFile --env-file $EnvFile down --remove-orphans | Out-Null
} catch {
    Write-Host "WARN: compose cleanup adimi basarisiz oldu, devam ediliyor."
}
docker compose -f $ComposeFile --env-file $EnvFile up -d db | Out-Null
docker compose -f $ComposeFile --env-file $EnvFile run --rm migrate | Out-Null
docker compose -f $ComposeFile --env-file $EnvFile up -d --build api web | Out-Null

Write-Host "[3/7] Hazirlik kontrolu"
try {
    Wait-HttpOk -Url "$ApiUrl/health" -Name "API health"
} catch {
    Wait-HttpOk -Url "$ApiUrl/swagger" -Name "API swagger"
}
Wait-HttpOk -Url $WebUrl -Name "Web UI"

Write-Host "[4/7] Smoke test"
$smokeFailed = $false
try {
    if ([string]::IsNullOrWhiteSpace($smokeEmail) -or [string]::IsNullOrWhiteSpace($smokePassword)) {
        throw "SMOKE_TEST_EMAIL ve SMOKE_TEST_PASSWORD ortam degiskenleri zorunlu."
    }
    powershell -NoProfile -ExecutionPolicy Bypass -File "ops/smoke-test.ps1" -BaseUrl $ApiUrl -Email $smokeEmail -Password $smokePassword
} catch {
    $smokeFailed = $true
    Write-Host "ERROR: Smoke test failed."
}

Write-Host "[5/7] Sample-data imza kontrolu"
$pdfBytes = [System.IO.File]::ReadAllBytes((Join-Path $rootDir "sample-data/sample-candidate.pdf"))
$docxBytes = [System.IO.File]::ReadAllBytes((Join-Path $rootDir "sample-data/sample-candidate.docx"))
if ([System.Text.Encoding]::ASCII.GetString($pdfBytes,0,5) -ne "%PDF-") { throw "PDF imzasi gecersiz" }
if ([System.Text.Encoding]::ASCII.GetString($docxBytes,0,2) -ne "PK") { throw "DOCX imzasi gecersiz" }
Write-Host "Sample-data imzalari OK"

Write-Host "[6/7] Evidence snapshot"
$evidenceFailed = $false
try {
    powershell -NoProfile -ExecutionPolicy Bypass -File "ops/evidence.ps1" -ApiBaseUrl $ApiUrl
} catch {
    $evidenceFailed = $true
    Write-Host "WARN: Evidence toplama adimi failed."
}

Write-Host "[7/7] Hazir"
Write-Host "NEXT: $WebUrl"
Write-Host "Open $WebUrl"
Write-Host "Next: follow docs/demo-seed-5min.md"

if ($smokeFailed -or $evidenceFailed) {
    throw "Run-demo failed: smoke=$smokeFailed evidence=$evidenceFailed"
}
