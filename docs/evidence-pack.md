# Evidence Pack (Juri Kanit Paketi)

Bu sayfa, demo sirasinda "hangi gereksinimin kaniti nerede?" sorusunu tek yerden cevaplamak icin hazirlandi.

## 1) Demo Quick Links
- Web ana: `http://localhost:3000`
- Swagger: `http://localhost:8080/swagger`
- Jobs: `http://localhost:3000/jobs`
- Candidates: `http://localhost:3000/candidates`

Demo sirasinda id bazli linkler:
- Job detail (weights/matches/applications): `http://localhost:3000/jobs/{jobId}`
- Candidate detail (cv/profile/ai-eval): `http://localhost:3000/candidates/{candidateId}?jobId={jobId}`
- Interview (chat/ai-debug/scorecard): `http://localhost:3000/interviews/{sessionId}?jobId={jobId}&candidateId={candidateId}&appId={applicationId}`

Not: Swagger export adimi best-effort calisir. API erisimi/Swagger generation sorunu varsa `swagger.warn.txt` uretir ve evidence toplama diger adimlarla devam eder.

## 2) Hard Requirement Kanitlari

### 2.1 Evidence yoksa score uretmeme
- Demo adimi: Interview -> `Run Auto Score` -> kriterlerde `INSUFFICIENT_EVIDENCE` etiketi gor.
- Test kaniti: `AutoScore_EvidenceMissing_WritesInsufficient_NotZeroScore`
- Kod kaniti: `backend/Services/InterviewScoringService.cs`

### 2.2 Strict schema validation + fallback
- Demo adimi: Adaptive/LLM akisi, invalid schema/guardrail durumunda fallback davranisi.
- Test kaniti:
- `StructuredJsonValidator_StrictRules_FailsOnEnumRange`
- `PlanSchemaInvalid_TriggersFallback`
- `TopicNotAllowed_TriggersGuardrailBlockAndFallback`
- Kod kaniti:
- `backend/Services/StructuredJsonValidator.cs`
- `backend/Services/InterviewOrchestratorService.cs`

### 2.3 Audit middleware order ve unauthorized mutating audit
- Demo adimi: token olmadan mutating endpoint denemesi -> 401/403 + audit kaydi.
- Test kaniti: `AuditMiddleware_LogsUnauthorizedMutating_WhenEnabled`
- Kod kaniti:
- `backend/Program.cs` (middleware sirasi)
- `backend/Middleware/AuditMiddleware.cs`

### 2.4 Migration uygulama kaniti
- Operasyon kaniti: `migrate` service logu.
- Komut: `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- Artefakt: `evidence/<timestamp>/migrations.txt`
- Not: yerel EF komutlari icin once `dotnet tool restore` calistirilir (`dotnet-ef 8.0.11`).

### 2.5 Vulnerability fix kaniti
- Komut: `dotnet list package --vulnerable --include-transitive`
- Beklenti: High severity yok.
- Artefakt: `evidence/<timestamp>/vuln.txt`

## 3) CI Kanitlari
- Workflow: `.github/workflows/ci.yml`
- Beklenen job kapsami:
- build/test
- migration drift
- vulnerability scan
- frontend build
- Yerel toplama scriptleri:
- Windows (PowerShell): `pwsh -File ops/evidence.ps1`
- Windows (PowerShell 5 alternatif): `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1`
- Linux/Mac: `bash ops/evidence.sh`
- Test artefaktlari:
- `evidence/<timestamp>/tests/unit/unit.trx`
- `evidence/<timestamp>/tests/integration/integration.trx`
- (Tek proje fallback) `evidence/<timestamp>/tests/tests-<timestamp>.trx`

## 4) Riskler ve Mitigation
- LLM provider gecikmesi/hatasi -> deterministic fallback + `LLM_FALLBACK_USED` audit.
- Yetki kaynakli demo kesintisi (403) -> admin kullanici ile demo.
- Rate limit (429) -> demo tempo dusur, policy/env kontrol et.
- Migration atlama riski -> her deploy oncesi `migrate` service zorunlu.

## 5) Ekran/Kanit Dosya Konumlari
- Screenshot plani: `docs/screenshots.md`
- Sample payload/CV: `sample-data/`
- Otomatik kanit output: `evidence/YYYYMMDD-HHMM/`
- Correlation kaniti: `docs/correlation-proof.md`
- Screenshot: `10-correlation-toast.png`

## 6) Dosya Imzasi Notu (CV)
- `sample-data/sample-candidate.pdf` basligi `%PDF-`
- `sample-data/sample-candidate.docx` basligi `PK` (zip container)
- Bu iki dosya upload tarafindaki magic-bytes kontrolune kanit amacli eklenmistir.
- Hizli dogrulama:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
- Linux/Mac: `bash ops/verify-sample-files.sh`
