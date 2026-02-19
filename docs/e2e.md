# E2E (Playwright)

## Gereksinimler
- Frontend + backend ayakta olmali (`ops/run-demo.*` veya esdeger).
- Browser binaries yuku:
```bash
npx playwright install
```

## Env
`frontend/.env.e2e.example` dosyasini referans alin:
- `E2E_USER`
- `E2E_PASS`
- `PLAYWRIGHT_BASE_URL` (default `http://localhost:3000`)

## Calistirma
Linux/Mac:
```bash
cd frontend
PLAYWRIGHT_BASE_URL=http://localhost:3000 E2E_USER=admin@local.test E2E_PASS=Admin123! npx playwright test
```

PowerShell:
```powershell
cd frontend
$env:PLAYWRIGHT_BASE_URL="http://localhost:3000"
$env:E2E_USER="admin@local.test"
$env:E2E_PASS="Admin123!"
npx playwright test
```

## Kapsam
- `e2e/auth.spec.ts`: login -> jobs sayfasi
- `e2e/jobs.spec.ts`: job create -> listede gorunur
- `e2e/interview.spec.ts`: candidate create -> job'a ekle -> interview ac
