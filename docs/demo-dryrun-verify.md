# Demo Dry-Run Verify Checklist

## 1) JWT Secret
- `.env.prod` dosyasinda `JWT_SECRET` dolu ve en az 32 karakter.
- Varsayilan/deger placeholder (`change_me_minimum_32_chars_secret_value`) kullanilmiyor.

## 2) Migrate
- Komut basarili: `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- `Program.cs` tarafinda `JWT_SECRET is required` hatasi alinmiyor.

## 3) Run-Demo End-to-End
- Linux/Mac: `bash ops/run-demo.sh`
- Windows: `powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1`
- Beklenen: db -> migrate -> api/web -> smoke -> evidence adimlari PASS.

## 4) Evidence Outputs
- `evidence/<timestamp>/migrations.txt`
- `evidence/<timestamp>/vuln.txt`
- Test raporu:
- `evidence/<timestamp>/tests/unit/unit.trx`
- `evidence/<timestamp>/tests/integration/integration.trx`
- veya fallback: `evidence/<timestamp>/tests/tests-<timestamp>.trx`

## 5) Sample-Data Verify
- Windows: `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
- Linux/Mac: `bash ops/verify-sample-files.sh`
- Beklenen: `%PDF-` ve `PK` imza kontrolu PASS.

## 6) JobDetail 403 UX
- `/jobs/{id}` acik, `/applications?jobId=...` 403 senaryosunda:
- Applications panelinde tek mesaj: `Bu panel icin yetkin yok.`
- Generic error + JsonErrorBox ayni anda gorunmuyor.
- Sayfanin diger panelleri calismaya devam ediyor.
