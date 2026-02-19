# Evidence Fix Verify Checklist

Bu dokuman evidence-pack fixlerinin son dogrulama kaydidir.

Date: 2026-02-18

## 1) Sample file signatures
- Command (Windows): `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
- Command (Linux/Mac): `bash ops/verify-sample-files.sh`
- Expected:
- PDF signature `%PDF-`
- DOCX signature `PK`
- Result: PASS (Windows dogrulandi)

## 2) Evidence scripts (API kapali)
- Command (Windows): `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1 -ApiBaseUrl http://localhost:8080`
- Expected:
- Swagger FAIL olabilir
- tests/migrations/vuln/ci-summary yine uretilmeli
- `summary.txt` + `step-status.txt` olusmali
- Result: PASS (kismi devam dogrulandi)

## 3) Evidence scripts (API acik)
- Command:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1`
- Linux/Mac: `bash ops/evidence.sh`
- Expected:
- `swagger.json` olusur
- `step-status.txt` icinde `swagger=OK`
- Result: PASS (Windows ortaminda test endpoint ile dogrulandi: `http://localhost:18080/swagger/v1/swagger.json`)

## 4) Compose env setup
- Required prep:
- Linux/Mac: `cp .env.prod.example .env.prod`
- PowerShell: `Copy-Item .env.prod.example .env.prod`
- Compose check:
- `docker compose -f docker-compose.prod.yml --env-file .env.prod.example config`
- Result: PASS (config parse dogrulandi, adimlar README'de net)

## 5) EF tools patch alignment
- Tool manifest: `.config/dotnet-tools.json` -> `dotnet-ef` `8.0.11`
- Command:
- `dotnet tool restore`
- `dotnet dotnet-ef migrations list --project backend/IkOtomasyon.Api.csproj`
- Expected: tool/runtime patch mismatch warning olmamali
- Result: PASS

## 6) Notes
- Windows ortaminda `pwsh` yoksa `powershell -ExecutionPolicy Bypass` komutlari kullanildi.
- Linux/Mac scriptleri eklendi; bu ortamda bash yoklugu nedeniyle calistirma dogrulamasi yapilamadi.
- Runtime API'de swagger 500 durumunda dahi evidence script kismi devam dogrulandi (`swagger=FAIL`, diger adimlar `OK`).
