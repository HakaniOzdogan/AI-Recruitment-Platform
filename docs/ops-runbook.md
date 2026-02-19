# Ops Runbook

## 1) Tek komut full stack (run-demo)
- Windows (PowerShell):
```powershell
$env:SMOKE_TEST_EMAIL="smoke-admin@local.test"
$env:SMOKE_TEST_PASSWORD="Admin123!"
powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1
```
- Linux/Mac:
```bash
SMOKE_TEST_EMAIL=smoke-admin@local.test SMOKE_TEST_PASSWORD=Admin123! bash ops/run-demo.sh
```

Run-demo akisi:
1. `.env.prod` yoksa `.env.prod.example` kopyalanir.
2. `JWT_SECRET` yok/placeholder ise guclu deger uretilir.
3. `db -> migrate -> api -> web` sirasi ile ayağa kalkar.
4. Ready check (API + Web) yapar.
5. Smoke (auth+jobs zorunlu) ve Evidence adimlarini calistirir.
6. Smoke/evidence fail ederse script non-zero code ile sonlanir.

Beklenen son mesaj:
- `Open http://localhost:3000`
- `Next: follow docs/demo-seed-5min.md`

## 2) Tek komut teslim paketi
- Windows (PowerShell):
```powershell
powershell -ExecutionPolicy Bypass -File ops/build-submission.ps1
```
- Linux/Mac:
```bash
bash ops/build-submission.sh
```

Beklenen ciktilar:
- Zip: `ik-otomasyon-submission-v{VERSION}.zip`
- Manifest: `submission/MANIFEST.txt`

## 3) Evidence ciktilari
- Klasor: `evidence/YYYYMMDD-HHMM/`
- Beklenen dosyalar:
- `swagger.json` (API ayaktaysa)
- `migrations.txt`
- `vuln.txt`
- `tests/unit/unit.trx`
- `tests/integration/integration.trx`
- (fallback) `tests/tests-<timestamp>.trx`

## 4) Temizlik
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod down -v
```
