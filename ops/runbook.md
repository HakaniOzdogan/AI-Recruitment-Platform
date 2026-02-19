# IK Otomasyon Runbook

## 1) Startup checklist
- `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d db`
- `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api web`
- `db` healthy mi (`docker ps`, `pg_isready`)?
- `JWT_SECRET`, `DB_CONNECTION`, `RATE_LIMIT_ENABLED` yuklendi mi?
- `cv_storage` volume mount edildi mi (`/app/storage`)?
- Web runtime config dogru mu?
- `docker exec ik-web-prod cat /usr/share/nginx/html/config.js`

## 2) Incident checklist

### 5xx artisi
- Correlation id ile log filtrele.
- DB health/connection limit/timeout kontrol et.
- Son migration ve deploy adimini kontrol et.
- LLM kaynakliysa circuit breaker/fallback loglarini kontrol et.
- Gecici mitigation: `LLM_ENABLED=false`.

### LLM sorunlari
- `LLM_PROVIDER`, `LLM_MODEL`, `LLM_API_KEY` degerlerini dogrula.
- Audit eventleri kontrol et:
- `LLM_GUARDRAIL_BLOCK`
- `LLM_FALLBACK_USED`

### Rate limit sikayetleri
- `RATE_LIMIT_ENABLED` ve rate ayarlarini kontrol et.
- 401/403 mutating denemelerinde audit artisi var mi incele.

## 3) Logs ve metrics
- Log alanlari: `correlationId`, `userId`, `route`, `status`, `latencyMs`
- Metric alanlari: `request_count`, `request_latency`, `llm_call_count`, `llm_latency`, `llm_failures`
- PII policy: CV icerigi ve prompt/response body loglanmaz.

## 4) Web deploy notlari
- Build: `docker build -t ik-web ./frontend`
- Run: `docker run --rm -p 3000:80 -e API_BASE_URL=/api ik-web`
- Nginx:
- SPA routing aktif (`try_files ... /index.html`)
- `index.html` ve `config.js` no-cache
- assets `1y immutable` cache
- security headers aktif (`nosniff`, `DENY`, `Referrer-Policy`)

## 5) Recovery / rollback
- EF rollback yerine `forward-fix` tercih edilir.
- Migration oncesi DB backup alin.
- Kritik migrationlarda kisa maintenance penceresi planlayin.