$ErrorActionPreference = "Stop"

Set-Location -Path $PSScriptRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  throw "Docker bulunamadi. Lutfen Docker Desktop kurup tekrar deneyin."
}

if (-not (Test-Path ".env")) {
  if (Test-Path ".env.example") {
    Copy-Item ".env.example" ".env"
    Write-Host ".env dosyasi olusturuldu (.env.example kopyalandi)."
  } else {
    throw ".env.example bulunamadi. Ortam degiskenlerini tanimlayacak bir .env dosyasi olusturun."
  }
}

Write-Host "Backend servisleri baslatiliyor: docker compose up --build -d postgres backend"
docker compose up --build -d postgres backend

# 5173 cakismasini engellemek icin docker frontend servisini kapali tut
docker compose stop frontend | Out-Null

# Frontend dev server (Vite)
$frontendPath = Join-Path $PSScriptRoot "frontend"
if (Test-Path (Join-Path $frontendPath "package.json")) {
  if (Get-Command npm -ErrorAction SilentlyContinue) {
    Write-Host "Frontend dev server baslatiliyor: npm run dev"
    Start-Process powershell -ArgumentList @(
      "-NoExit",
      "-Command",
      "Set-Location -Path '$frontendPath'; npm run dev"
    )
  } else {
    Write-Warning "npm bulunamadi. Frontend dev server baslatilamadi."
  }
} else {
  Write-Warning "frontend/package.json bulunamadi. npm run dev adimi atlandi."
}

# Servislerin ayaga kalkmasini bekle ve tarayiciyi ac
Start-Sleep -Seconds 5
Start-Process "http://localhost:8080/swagger"
Start-Process "http://localhost:8080/health"

$frontendReady = $false
for ($i = 0; $i -lt 20; $i++) {
  try {
    $resp = Invoke-WebRequest -Uri "http://127.0.0.1:5173" -UseBasicParsing -TimeoutSec 2
    if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
      $frontendReady = $true
      break
    }
  } catch {
    Start-Sleep -Seconds 1
  }
}

if ($frontendReady) {
  Start-Process "http://localhost:5173"
} else {
  Write-Warning "Frontend 5173 hazir degil. Acilan npm penceresindeki hatayi kontrol edin."
}
