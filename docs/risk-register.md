# Risk Register

| Risk | Likelihood | Impact | Mitigation | Owner | Next Action | Status |
|---|---|---|---|---|---|---|
| Frontend production image reproducibility (`npm ci` lockfile bagimliligi) | High | High | `package-lock.json` sabitleme veya Docker build adiminda lockfile policy netlestirme | Frontend Lead | v1.0.1 hotfix: lockfile ekle ve run-demo tekrar dogrula | Accepted |
| Smoke script portability (`Invoke-WebRequest` null reference) | Medium | Medium | `curl.exe` fallback/Invoke-RestMethod ile saglam endpoint check | Ops | v1.0.1 hotfix: smoke-test.ps1 endpoint kontrollerini guncelle | Accepted |
| Swagger JSON export hatasi (file upload action generation) | Medium | Medium | Swagger operation mapping fix + evidence fallback zaten aktif | Backend Lead | v1.0.1: swagger/v1 json 200 olacak sekilde duzelt | Accepted |
| LLM provider latency/downtime | Medium | High | Deterministic fallback + timeout/retry/circuit breaker, gerekirse `LLM_ENABLED=false` | Backend Lead | Aylik fallback test senaryosu kos | Accepted |
| Rate limit false positives | Medium | Medium | Endpoint bazli limit tuning, `429` izleme, runbook ayarlari | Ops | Ilk prod haftasi `429` trendini incele | Accepted |
| Data retention/anonymization operasyonu | Medium | High | Retention SOP + periyodik cleanup plani (gelecek is) | Product + Ops | v1.1 backlog item ac ve SOP onayla | Accepted |
| CV parsing edge-case dosyalar | High | Medium | Safe fail + parse error visibility + manuel fallback | Backend Lead | Yeni CV ornek havuzu ile regression testi | Accepted |
| Permission/role seed misconfiguration | Medium | Medium | Seed check + smoke + 403 UX kontrolu | QA + Ops | Deploy oncesi smoke checklist zorunlu kil | Mitigated |
| Environment misconfiguration (`.env.prod`, JWT, DB) | Medium | High | `ops/run-demo*`, `docs/troubleshooting.md`, pre-freeze check | DevOps | Release oncesi pre-freeze-check calistir | Mitigated |
| Migration drift | Low | High | Deploy oncesi zorunlu `migrate` + CI drift check | DevOps | Her release'te migrate job logunu arsivle | Mitigated |
| Dependency security regressions | Low | Medium | CI vuln scan + release gate + patch takip | Tech Lead | Aylik package review gorevi planla | Mitigated |
