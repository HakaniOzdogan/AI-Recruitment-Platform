# Cross-Platform Komut Tablosu

| Islem | Windows (PowerShell) | Linux/Mac (bash) |
|---|---|---|
| Env olusturma | `Copy-Item .env.prod.example .env.prod` | `cp .env.prod.example .env.prod` |
| Run Demo | `$env:SMOKE_TEST_EMAIL='smoke-admin@local.test'; $env:SMOKE_TEST_PASSWORD='Admin123!'; powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1` | `SMOKE_TEST_EMAIL=smoke-admin@local.test SMOKE_TEST_PASSWORD=Admin123! bash ops/run-demo.sh` |
| Evidence | `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1` | `bash ops/evidence.sh` |
| Smoke | `powershell -ExecutionPolicy Bypass -File ops/smoke-test.ps1 -BaseUrl http://localhost:8080 -Email admin@local.test -Password Admin123!` | `BASE_URL=http://localhost:8080 SMOKE_TEST_EMAIL=admin@local.test SMOKE_TEST_PASSWORD=Admin123! bash ops/smoke-test.sh` |
| Build Submission | `powershell -ExecutionPolicy Bypass -File ops/build-submission.ps1` | `bash ops/build-submission.sh` |
| Compose Up (db) | `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d db` | `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d db` |
| Compose Migrate | `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate` | `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate` |
| Compose Up (api+web) | `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api web` | `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api web` |
| Compose Down/Cleanup | `docker compose -f docker-compose.prod.yml --env-file .env.prod down -v` | `docker compose -f docker-compose.prod.yml --env-file .env.prod down -v` |
