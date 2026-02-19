# IK Otomasyon (AI Destekli IK Otomasyonu)

Bu repo, ic kullanim odakli AI destekli IK otomasyonunun tam-stack demo surumudur. Auth/RBAC, ATS, CV pipeline, matching, interview, scorecard ve AI evaluation akislarini tek sistemde calistirir.

Project status: **v1.0.0 released**.

## Architecture (kisa)
- `backend/`: .NET 8 Web API (JWT, RBAC, ATS, CV parse/profile, Matching, Interview, Scoring, Audit)
- `frontend/`: React + TypeScript + Vite SPA
- `db`: PostgreSQL
- `web`: Nginx static hosting + runtime `config.js`
- `llm`: provider-agnostic client (flag ile ac/kapat)

## Gereksinimler
- Docker + Docker Compose (onerilen yol)
- Opsiyonel local dev: `.NET SDK 8`, `Node.js 20+`

## Quickstart

### A) Sadece backend (dev)
1. `dotnet restore *.sln`
2. `dotnet build *.sln`
3. `dotnet run --project backend/IkOtomasyon.Api.csproj`

### B) Backend + DB (compose)
1. Env dosyasini olustur:
   - Linux/Mac: `cp .env.prod.example .env.prod`
   - PowerShell: `Copy-Item .env.prod.example .env.prod`
2. `.env.prod` icindeki degerleri doldur.
3. `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d db`
4. `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
5. `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api`

### C) Full stack (backend + db + web)
1. Env dosyasini olustur:
   - Linux/Mac: `cp .env.prod.example .env.prod`
   - PowerShell: `Copy-Item .env.prod.example .env.prod`
2. `.env.prod` icindeki degerleri doldur.
3. `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d db`
4. `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
5. `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api web`

Tek komut dry-run:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1`
- Linux/Mac: `bash ops/run-demo.sh`

Erisim:
- API Swagger: `http://localhost:8080/swagger`
- API Health: `http://localhost:8080/health`
- Web UI: `http://localhost:3000`

## Konfigurasyon (kritik)
- `JWT_SECRET`: zorunlu, minimum 32 karakter (Production fail-fast).
- `DB_CONNECTION`: zorunlu.
- LLM flagleri: `LLM_ENABLED`, `ADAPTIVE_INTERVIEW_ENABLED`, `LLM_SCORING_ENABLED`
- Web runtime API: `WEB_API_BASE_URL=/api` (onerilen same-origin proxy)

## Migration stratejisi
- Onerilen yol `migrate` service:
- `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- API servisi `migrate` basarisizsa ayaga kalkmaz.
- EF tools kullanmadan once: `dotnet tool restore`

## Demo setup ve smoke
- Hizli demo setup: `bash ops/demo-setup.sh`
- API smoke test: `bash ops/smoke-test.sh`
- Ornek:
- `BASE_URL=http://localhost:8080 SMOKE_TEST_EMAIL=admin@local.test SMOKE_TEST_PASSWORD=Admin123! bash ops/smoke-test.sh`

## Demo verisi
- Development ortaminda ilk admin seed otomatik gelebilir.
- Production-benzeri compose ortaminda otomatik admin seed garanti degildir.
- Demo verisini UI akisiyla olusturman onerilir: Job -> Candidate -> CV parse -> Application -> Interview.
- `ops/demo-setup.sh` altyapiyi ayaga kaldirir; domain verisini olusturmaz.
- Ornek CV dosyalari: `sample-data/sample-candidate.pdf` ve `sample-data/sample-candidate.docx`
- Upload tarafinda magic-bytes kontrolu oldugu icin bu dosyalar gecerli imzalarla gelir (`%PDF-`, `PK`).
- Imza dogrulama:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
- Linux/Mac: `bash ops/verify-sample-files.sh`

## Teslim dokumanlari
- Demo akisi (10 dk): `docs/demo-script.md`
- Demo seed: `docs/demo-seed.md`
- Failure-case adimlari: `docs/demo-failure-cases.md`
- Komut tablosu: `docs/commands.md`
- Teknik ozet (3 dk): `docs/architecture-3min.md`
- Smoke checklist: `docs/demo-smoke.md`
- Troubleshooting: `docs/troubleshooting.md`
- Ops runbook: `ops/runbook.md`
- Acceptance sign-off: `docs/acceptance-signoff.md`
- Risk register: `docs/risk-register.md`
- Freeze procedure: `docs/freeze.md`
- Submission notes: `docs/submission-notes.md`
