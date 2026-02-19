# Changelog

All notable changes to this project are documented in this file.

## [1.0.0] - 2026-02-18
### Added
- Auth + JWT login/refresh + permission-based RBAC core.
- ATS core: Jobs, Candidates, Applications, Pipeline.
- CV pipeline: upload, parse, profile extraction.
- Matching: deterministic score + reasons/gaps + weights.
- Interview flow: chat, modes (OFF/ASSIST/ADAPTIVE), AI debug visibility.
- Rubric scoring: auto score, insufficient evidence handling, human override.
- AI CV evaluation report flow with consent gate.
- Audit logging and correlation-id visibility.
- Demo UI with end-to-end internal HR flow.

### Security / Ops
- Rate limiting, resilience policies, security headers, env-based secrets.
- Production compose flow (`db -> migrate -> api -> web`).
- CI gates: build/test, migration drift, vulnerability checks.
- Release/readiness docs, smoke and evidence scripts.

### Fixes
- JWT secret production startup alignment (`JWT_SECRET`) for migrate/runtime flow.
- Evidence scripts hardened for cross-platform execution and partial-failure continuation.
- Sample CV files replaced with real-signature PDF/DOCX for upload magic-bytes checks.
- UI partial render behavior improved for `403` cases in panel-level flows.
- Submission bundle excludes validated (`.env*`, `evidence/`, `node_modules/`, `dist/`).

### Packaging
- Frontend production Docker image with nginx and runtime config override.
- Evidence pack, final report set, demo/failure runbooks.

### Known Limitations
- Scraping yok (LinkedIn/Kariyer vb. dis veri cekimi yok).
- Dashboard/BI katmani yok (operational metrics API/disarida).
- Advanced analytics/prediction modulleri yok (MVP kapsam disi).

## Release Tag (manual)
- Suggested tag: `v1.0.0`
- Commands:
  - `git tag -a v1.0.0 -m "Release 1.0.0"`
  - `git push origin v1.0.0`
