# Go-Live Checklist

## 1) Pre-Deploy
- Branch protection aktif:
  - `build_test`, `migrations_drift`, `security_vuln` joblari **required** olmadan merge yok.
- `.env.prod` hazir:
  - `JWT_SECRET` (min 32 char random)
  - `DB_CONNECTION`
  - `LLM_*` degiskenleri (kullaniliyorsa)
  - `RATE_LIMIT_ENABLED=true`
- DB backup alindi.
- Release gate local/runner’da calistirildi:
  - `bash ops/release-gate.sh`

## 2) Deploy
- Migration once:
  - `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- Servisleri ayağa kaldır:
  - `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api db`

## 3) Post-Deploy
- Smoke test:
  - `bash ops/smoke-test.sh` (gerekirse `SMOKE_RUN_MIGRATE=false`)
- Security check:
  - `bash ops/security-check.sh`
- Logs:
  - startup exception yok
  - correlation id ile request akisi izlenebiliyor
- Rate limit ve security headers aktif.
- LLM rollout:
  - once `LLM_ENABLED=false`
  - sistem stabilse kademeli `true`.
