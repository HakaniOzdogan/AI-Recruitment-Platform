param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$Email = $env:SMOKE_TEST_EMAIL,
    [string]$Password = $env:SMOKE_TEST_PASSWORD
)

$ErrorActionPreference = "Stop"
$warnings = New-Object System.Collections.Generic.List[string]
$failures = New-Object System.Collections.Generic.List[string]

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = "http://localhost:8080"
}

function Get-HttpStatusCode {
    param([string]$Url)
    try {
        $code = & curl.exe -s -o NUL -w "%{http_code}" $Url
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($code)) {
            return 0
        }
        return [int]$code
    } catch {
        return 0
    }
}

Write-Host "[1/4] Swagger kontrolu..."
$swaggerUrls = @("$BaseUrl/swagger/v1/swagger.json", "$BaseUrl/swagger")
$swaggerOk = $false
foreach ($url in $swaggerUrls) {
    $statusCode = Get-HttpStatusCode -Url $url
    if ($statusCode -ge 200 -and $statusCode -lt 400) {
        $swaggerOk = $true
        break
    } else {
        $warnings.Add("Swagger endpoint erisilemedi: $url")
    }
}
if (-not $swaggerOk) {
    $warnings.Add("Swagger kontrolu warning olarak gecildi.")
}

Write-Host "[2/4] Health kontrolu..."
$healthStatus = Get-HttpStatusCode -Url "$BaseUrl/health"
if ($healthStatus -ne 200) {
    $failures.Add("Health kontrolu basarisiz: $healthStatus")
}

Write-Host "[3/4] Auth login kontrolu..."
if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
    $failures.Add("SMOKE_TEST_EMAIL ve SMOKE_TEST_PASSWORD zorunlu. Auth/jobs adimi atlanamaz.")
} else {
    try {
        $loginBody = @{
            email = $Email
            password = $Password
        } | ConvertTo-Json
        $login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -TimeoutSec 20
        if ([string]::IsNullOrWhiteSpace($login.accessToken)) {
            $failures.Add("Login basarisiz: accessToken alinamadi.")
        } else {
            Write-Host "[4/4] Jobs endpoint kontrolu..."
            $headers = @{ Authorization = "Bearer $($login.accessToken)" }
            try {
                Invoke-RestMethod -Uri "$BaseUrl/jobs" -Method GET -Headers $headers -TimeoutSec 20 | Out-Null
            } catch {
                $failures.Add("Jobs kontrolu basarisiz: $($_.Exception.Message)")
            }
        }
    } catch {
        $failures.Add("Auth login basarisiz: $($_.Exception.Message)")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Smoke test FAIL"
    $failures | ForEach-Object { Write-Host "- $_" }
    if ($warnings.Count -gt 0) { $warnings | ForEach-Object { Write-Host "- WARN: $_" } }
    exit 1
} elseif ($warnings.Count -gt 0) {
    Write-Host "Smoke test WARN"
    $warnings | ForEach-Object { Write-Host "- $_" }
} else {
    Write-Host "Smoke test PASS"
}
