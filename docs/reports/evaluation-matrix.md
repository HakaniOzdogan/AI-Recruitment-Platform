# Degerlendirme Matrisi

| Requirement | Implementation (dosya/servis) | Verification (test/endpoint/demo) | Status |
|---|---|---|---|
| JWT login + refresh | `backend/Controllers/AuthController.cs`, `backend/Services/AuthService.cs` | `/auth/login`, `/auth/refresh`, Swagger login | Done |
| Permission-based RBAC | `backend/Authorization/RequirePermission*`, `PermissionService` | 401/403 integration testleri | Done |
| Audit log altyapisi | `backend/Middleware/AuditMiddleware.cs` | Mutating request audit kontrolu | Done |
| ATS core (job/candidate/application/pipeline) | `JobsController`, `CandidatesController`, `ApplicationsController` | Job->Candidate->Application demo akisi | Done |
| Job state machine (Draft->Published->Closed) | Job business rule katmani | Publish/close endpoint testleri | Done |
| CV upload/parse/profile | `Cv*` endpointleri + parser servisleri | Upload->Parse->Profile smoke adimlari | Done |
| Matching reasons/gaps | `MatchService`, `MatchResult` | `/jobs/{jobId}/matches` + UI shortlist | Done |
| Weights auth korumasi | `backend/Controllers/JobsController.cs` (`RequirePermission`) | `GET /jobs/{id}/weights` 401/403/200 testi | Done |
| Evidence yoksa score uretmeme | `backend/Services/InterviewScoringService.cs`, `InterviewCriterionScore.Status` | Auto score + `INSUFFICIENT_EVIDENCE` kontrolu | Done |
| Human override whitespace evidence => 400 | `InterviewScoringService` validation + exception mapping | Human override validation integration testi | Done |
| Adaptive interview strict schema + fallback | `InterviewOrchestratorService`, `StructuredJsonValidator`, `RealLLMClient` | Schema invalid/guardrail fallback testleri | Done |
| Guardrail + fallback cift audit | Interview flow audit yazimi | `LLM_GUARDRAIL_BLOCK` + `LLM_FALLBACK_USED` birlikte kontrol | Done |
| Unauthorized mutating audit | `Program.cs` middleware order + `AuditMiddleware` | 401/403 mutating deneme audit testi | Done |
| LLM-assisted scoring enrichment | `InterviewScoringEnrichmentService` + LLM config | Enrichment success/fail fallback testleri | Done |
| Rate limiting + resilience | `Program.cs` rate limiter, LLM policy config | 429 davranisi, timeout/circuit breaker testleri | Done |
| CorrelationId gorunurlugu | Frontend axios interceptor + backend logs | UI hata toast `Ref:<id>` + log esleme | Done |
| Migration strategy (migrate job) | `docker-compose.prod.yml` `migrate` servisi | Bos DB'de migrate->api startup | Done |
| Vulnerability gate | CI workflow + package updates | `dotnet list package --vulnerable` high fail gate | Done |
| Frontend demo modulleri | `frontend/src/pages/*`, endpoint wrappers | Uctan uca demo smoke (jobs/candidates/interview/score) | Done |
| Release gate + runbook | `ops/release-gate.sh`, `ops/runbook.md`, `docs/go-live.md` | Local release-gate + post-deploy smoke | Done |

## Not
- Kanit kaynaklari: Swagger endpointleri, integration testler, `docs/demo-smoke.md` adimlari, CI ciktilari.
- Durum alanlari gerekirse `Partial` olarak guncellenebilir.