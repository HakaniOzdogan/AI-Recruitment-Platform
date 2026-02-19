# Acceptance Sign-off

Date: 2026-02-19
Release: `1.0.0`

Status kullanimi:
- `PASS` = kabul edildi
- `FAIL` = bloklayici sorun var

## 1) Functional
| Requirement | Status (PASS/FAIL) | Evidence (docs/test/screenshot) | Notes |
|---|---|---|---|
| Jobs (create/list/publish/weights) | PASS | `docs/demo-10min-cheatsheet.md`, `docs/ui-happy-path.md`, `docs/screenshots.md` (`screenshots/01-jobs.png`) | UI akista dogrulandi |
| Candidates (create/list) | PASS | `docs/demo-seed-5min.md`, `docs/ui-smoke.md` | Empty-state CTA mevcut |
| CV upload/parse/profile | PASS | `sample-data/sample-candidate.pdf`, `sample-data/sample-candidate.docx`, `docs/ui-smoke.md` | Sample imza dogrulamasi gecti |
| Matching shortlist (reasons/gaps) | PASS | `docs/demo-script.md`, `docs/ui-smoke.md`, `docs/screenshots.md` (`screenshots/02-job-detail.png`) | minScore + reasons/gaps gorunur |
| Applications + stage updates | PASS | `docs/ui-happy-path.md`, `docs/demo-script.md` | 409 duplicate davranisi net |
| Interview chat flow | PASS | `docs/ui-happy-path.md`, `docs/screenshots.md` (`screenshots/03-interview.png`) | load/send/refresh dogrulandi |
| Adaptive mode + fallback görünürlük | PASS | `docs/demo-failure-cases.md`, `docs/screenshots.md` (`screenshots/03-interview.png`) | 409 + debug flags gorunur |
| Scoring auto + insufficient evidence | PASS | `evidence/20260219-1055/tests/unit/unit.trx`, `evidence/20260219-1055/tests/integration/integration.trx`, `docs/ui-happy-path.md` | Insufficient evidence davranisi testte mevcut |
| Human override (validation dahil) | PASS | `evidence/20260219-1055/tests/unit/unit.trx`, `evidence/20260219-1055/tests/integration/integration.trx`, `docs/ui-happy-path.md` | 400 validasyon davranisi korunuyor |
| AI evaluation latest + force | PASS | `docs/demo-script.md`, `docs/ui-smoke.md` | consent/forbidden/error state mevcut |

## 2) Security / Ops
| Requirement | Status (PASS/FAIL) | Evidence (docs/test/script) | Notes |
|---|---|---|---|
| RBAC / permission checks (401/403) | PASS | Integration tests, `docs/evidence-pack.md` | 401/403 davranisi net |
| `/jobs/{id}/weights` authorization | PASS | Integration test: `JobsWeights_Get_RequiresPermission` | Yetkisiz erisim engelli |
| Secrets via env + JWT fail-fast | PASS | `README.md`, `docs/troubleshooting.md`, `ops/run-demo.*` | JWT_SECRET olmadan deploy bloklu |
| Vulnerability scan gate (High=0 hedefi) | PASS | `evidence/20260219-1055/vuln.txt`, CI workflow | High yok |
| Rate limiting + security headers | PASS | `docs/go-live.md`, `ops/security-check.sh` | Prod checklistte mevcut |
| Audit log + correlationId visibility | PASS | `docs/correlation-proof.md`, `docs/evidence-pack.md` | UI Ref + log eslestirme var |

## 3) Quality / Release
| Requirement | Status (PASS/FAIL) | Evidence | Notes |
|---|---|---|---|
| Frontend Docker image build | PASS | `docker build -t ik-web ./frontend` (2026-02-19) | `npm ci` lockfile ile basarili build |
| Unit + integration tests | PASS | `evidence/20260219-1055/tests/unit/unit.trx`, `evidence/20260219-1055/tests/integration/integration.trx` | Ayrik TRX ciktilari olusuyor |
| CI gates aktif | PASS | `.github/workflows/ci.yml` | build/test + drift + vuln |
| run-demo scripts ready | PASS | `ops/run-demo.ps1` run log (2026-02-19 10:46), `docker-compose.prod.yml`, `frontend/Dockerfile` | zincir calisiyor (db->migrate->api->web) |
| smoke + evidence scripts ready | PASS | `ops/smoke-test.ps1`, `evidence/20260219-1055/swagger.json`, `evidence/20260219-1055/summary.txt` | Smoke WARN (401 login) ile patlamadan tamamlandi |
| Submission bundle üretimi | PASS | `ops/build-submission.sh`, `ops/build-submission.ps1`, `submission/MANIFEST.txt`, `ik-otomasyon-submission-v1.0.0.zip` | secrets/exclude kurallari var |
| UI polish evidence | FAIL | `docs/screenshots.md`, `screenshots/01-jobs.png`, `screenshots/02-job-detail.png`, `screenshots/03-interview.png`, `deliverables/SCREENSHOT_STATUS.txt` | Mevcut dosyalar placeholder; canli UI capture ile yenilenmeli |

## 4) Signatures
- Product/Project Owner: ____________________  Date: __________
- Tech Lead: ________________________________  Date: __________
- QA/Reviewer: ______________________________  Date: __________
