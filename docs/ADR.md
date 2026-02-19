# ADR

## Decisions
- CV files are stored in local filesystem storage (`CvStorage:RootPath`).
- CV parsing runs synchronously for MVP.
- No external platform scraping is used; evaluation input is limited to internal job + profile + matching data.
- AI evaluation is consent-gated (`CandidateConsent`) and versioned (`AiEvaluationReport.Version`).
- LLM integration is provider-agnostic via `ILLMClient` interface; `MockLLMClient` is used for deterministic MVP/demo behavior.
- Job-level weighted criteria are explicit and persisted:
  - `JobPosting.CompetencyWeightsJson`
  - `JobSkillWeight` records
- Adaptive interview follows a controlled two-step LLM flow:
  - `ANALYZE` (signals/competency/depth/risk/evidence)
  - `PLAN` (2-3 next questions)
- Adaptive mode is guardrailed with allowlist categories/topics and deterministic fallback.

## Status
- Accepted (MVP)

## Context
- Internal HR usage requires explainable shortlist support without data compliance risk.
- Team needs deterministic demo behavior before enabling a real LLM provider.
- Recruiters need configurable weighting per job to align role-specific expectations.
- Interview steering must stay explainable and auditable; free-form LLM question generation is not acceptable.

## Consequences
- Pros:
  - Clear data boundary (no scraping).
  - Explicit consent handling and auditability.
  - Configurable, repeatable job criteria and report version history.
  - Easy swap from mock to real provider without controller/service contract changes.
  - Every adaptive turn is traceable via `InterviewInsight` + `InterviewPlan`.
  - Failure-safe behavior: invalid LLM output falls back to deterministic template questions.
- Cons:
  - Mock outputs are limited compared to a production model.
  - JSON fields require strict validation discipline.
  - Synchronous paths can add latency under larger payloads.
  - Additional persistence footprint for per-turn insight and planning logs.

## Integration Test Notes
- Endpoint-level integration tests use `WebApplicationFactory<Program>` + PostgreSQL Testcontainers.
- Test project: `backend/tests/IntegrationTests`.
- Docker is required for integration tests.
- Recommended local run order:
  1. `dotnet build İk_otomasyon.sln`
  2. `dotnet test backend/tests/IkOtomasyon.Api.Tests/IkOtomasyon.Api.Tests.csproj --no-build`
  3. `dotnet test backend/tests/IntegrationTests/IkOtomasyon.Api.IntegrationTests.csproj --no-build`
- Flaky `CS2012` mitigation:
  - Keep test parallelization disabled for integration tests.
  - Prefer `--no-build` when running tests repeatedly.
  - If lock persists, close active test hosts / IDE test sessions and rerun.
