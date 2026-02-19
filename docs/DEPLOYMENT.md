# Deployment Notlari (Prod-benzeri)

## 1) Secrets ve Config
- Gercek secret dosyasi repo'ya konmaz.
- `.env.prod.example` referans alınarak `.env.prod` olusturulur.
- CI/CD tarafinda secret store (GitHub Actions Secrets, GitLab CI Variables, Vault) kullan.

Zorunlu kritik degiskenler:
- `DB_CONNECTION`
- `JWT_SECRET` (min 32 karakter)
- `ASPNETCORE_ENVIRONMENT=Production`

Opsiyonel:
- `LLM_ENABLED`, `LLM_PROVIDER`, `LLM_API_KEY`, `LLM_MODEL`
- `RATE_LIMIT_ENABLED`, `AUDIT_LOG_UNAUTHORIZED`

## 2) Docker Compose (prod-benzeri)
Calistirma:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

Migration'i ayri calistirmak istersen:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate
```

Ops panel (adminer) ile:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod --profile ops up -d
```

## 3) Migration Stratejisi

### Secenek 1 (MVP): Startup migrate (flag kontrollu)
- `APPLY_MIGRATIONS_ON_STARTUP=true` ise app startup'ta `db.Database.MigrateAsync()` calisir.
- Varsayilan production davranisi: `false`.

### Secenek 2: Ayrı migration adimi (onerilen prod)
- Deploy pipeline’da API acilmadan once migration komutu calistirilir:
```bash
dotnet ef database update --project backend/IkOtomasyon.Api.csproj --startup-project backend/IkOtomasyon.Api.csproj
```
- Sonra API release edilir.
- Compose ile migration job:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate
```

Durum dogrulama:
```bash
dotnet ef migrations list --project backend/IkOtomasyon.Api.csproj --startup-project backend/IkOtomasyon.Api.csproj
```
`(Pending)` gorunmemelidir.

## 5) CI Release Gates
- Workflow: `.github/workflows/ci.yml`
- Joblar:
  - `build_test`
  - `migrations_drift`
  - `security_vuln`
- Branch protection'ta bu joblar required olmadan merge/deploy yapilmaz.

## 4) Rollback Pratigi
- EF migration rollback her zaman guvenli degildir.
- Pratik yaklasim:
  1. Deployment oncesi DB backup.
  2. Sorunda forward-fix migration ile duzeltme.
  3. Kritik hatada backup restore + onceki image rollback.
