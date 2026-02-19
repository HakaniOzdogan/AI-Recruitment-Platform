#!/usr/bin/env bash
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"
TIMESTAMP="$(date +%Y%m%d-%H%M)"
OUT_DIR="${ROOT_DIR}/evidence/${TIMESTAMP}"
TEST_OUT_DIR="${OUT_DIR}/tests"
FAILURES=()
STEP_SWAGGER="FAIL"
STEP_TESTS="FAIL"
STEP_MIGRATIONS="FAIL"
STEP_VULN="FAIL"
STEP_CI="FAIL"

mkdir -p "${OUT_DIR}" "${TEST_OUT_DIR}"

echo "[1/6] Swagger export"
SWAGGER_OK=0
for CANDIDATE in "${API_BASE_URL}/swagger/v1/swagger.json" "${API_BASE_URL%/}/swagger/v1/swagger.json"; do
  for ATTEMPT in 1 2 3; do
    if curl -fsS --max-time 20 "${CANDIDATE}" -o "${OUT_DIR}/swagger.json"; then
      SWAGGER_OK=1
      break 2
    fi
    sleep 1
  done
done
if [[ "${SWAGGER_OK}" -eq 1 ]]; then
  STEP_SWAGGER="OK"
else
  echo "WARN: Swagger indirilemedi (${API_BASE_URL}). Devam ediliyor." | tee "${OUT_DIR}/swagger.warn.txt"
  FAILURES+=("swagger")
fi

echo "[2/6] Test reports"
if command -v dotnet >/dev/null 2>&1; then
  UNIT_PROJ="${ROOT_DIR}/backend/tests/IkOtomasyon.Api.Tests/IkOtomasyon.Api.Tests.csproj"
  INTEGRATION_PROJ="${ROOT_DIR}/backend/tests/IntegrationTests/IkOtomasyon.Api.IntegrationTests.csproj"
  TESTS_FAILED=0
  if [[ -f "${UNIT_PROJ}" && -f "${INTEGRATION_PROJ}" ]]; then
    mkdir -p "${TEST_OUT_DIR}/unit" "${TEST_OUT_DIR}/integration"
    dotnet test "${UNIT_PROJ}" -c Release --no-build --logger "trx;LogFileName=unit.trx" --results-directory "${TEST_OUT_DIR}/unit" || TESTS_FAILED=1
    dotnet test "${INTEGRATION_PROJ}" -c Release --no-build --logger "trx;LogFileName=integration.trx" --results-directory "${TEST_OUT_DIR}/integration" || TESTS_FAILED=1
  else
    TS="$(date +%Y%m%d-%H%M%S)"
    dotnet test -c Release --no-build --logger "trx;LogFileName=tests-${TS}.trx" --results-directory "${TEST_OUT_DIR}" || TESTS_FAILED=1
  fi

  if [[ "${TESTS_FAILED}" -ne 0 ]]; then
    FAILURES+=("tests")
  else
    STEP_TESTS="OK"
  fi
else
  echo "dotnet bulunamadi, test raporu alinmadi" > "${TEST_OUT_DIR}/warning.txt"
  FAILURES+=("tests")
fi

echo "[3/6] Migrations list"
if command -v dotnet >/dev/null 2>&1; then
  MIGRATIONS_OUTPUT_FILE="${OUT_DIR}/migrations.txt"
  if (cd "${ROOT_DIR}" && dotnet tool restore >/dev/null 2>&1; cd "${ROOT_DIR}/backend" && dotnet dotnet-ef migrations list) > "${MIGRATIONS_OUTPUT_FILE}" 2>&1; then
    if grep -Eiq "Pending status not shown|error occurred while accessing the database|Failed to connect" "${MIGRATIONS_OUTPUT_FILE}"; then
      if command -v docker >/dev/null 2>&1 && [[ -f "${ROOT_DIR}/docker-compose.prod.yml" && -f "${ROOT_DIR}/.env.prod" ]]; then
        MIGRATE_FALLBACK_FAILED=0
        {
          echo "---- fallback: docker compose migrate check ----"
          docker compose -f "${ROOT_DIR}/docker-compose.prod.yml" --env-file "${ROOT_DIR}/.env.prod" run --rm migrate
        } >> "${MIGRATIONS_OUTPUT_FILE}" 2>&1 || MIGRATE_FALLBACK_FAILED=1
        if [[ "${MIGRATE_FALLBACK_FAILED}" -eq 0 ]]; then
          STEP_MIGRATIONS="OK"
        else
          FAILURES+=("migrations")
        fi
      else
        FAILURES+=("migrations")
      fi
    else
      STEP_MIGRATIONS="OK"
    fi
  else
    FAILURES+=("migrations")
  fi
else
  echo "dotnet bulunamadi" > "${OUT_DIR}/migrations.txt"
  FAILURES+=("migrations")
fi

echo "[4/6] Vulnerability scan"
if command -v dotnet >/dev/null 2>&1; then
  if ! (cd "${ROOT_DIR}/backend" && dotnet list package --vulnerable --include-transitive) > "${OUT_DIR}/vuln.txt" 2>&1; then
    FAILURES+=("vuln-scan")
  else
    STEP_VULN="OK"
  fi
else
  echo "dotnet bulunamadi" > "${OUT_DIR}/vuln.txt"
  FAILURES+=("vuln-scan")
fi

echo "[5/6] CI summary"
if [[ -f "${ROOT_DIR}/.github/workflows/ci.yml" ]]; then
  {
    echo "CI workflow file: .github/workflows/ci.yml"
    echo "Expected jobs: build_test, migrations_drift, security_vuln, frontend build"
    echo "Run URL template: <repo>/actions/workflows/ci.yml"
  } > "${OUT_DIR}/ci-summary.txt"
  STEP_CI="OK"
else
  echo "ci.yml bulunamadi" > "${OUT_DIR}/ci-summary.txt"
fi

echo "[6/6] Index"
cat > "${OUT_DIR}/README.txt" <<EOF
Evidence bundle generated at: ${TIMESTAMP}
API base: ${API_BASE_URL}
Files:
- swagger.json
- migrations.txt
- vuln.txt
- ci-summary.txt
- tests/*.trx
EOF

if [[ ${#FAILURES[@]} -gt 0 ]]; then
  printf "%s\n" "Failures: ${FAILURES[*]}" > "${OUT_DIR}/summary.txt"
  echo "Completed with warnings. See ${OUT_DIR}/summary.txt"
else
  echo "All steps succeeded." > "${OUT_DIR}/summary.txt"
fi

cat > "${OUT_DIR}/step-status.txt" <<EOF
swagger=${STEP_SWAGGER}
tests=${STEP_TESTS}
migrations=${STEP_MIGRATIONS}
vuln-scan=${STEP_VULN}
ci-summary=${STEP_CI}
EOF

echo "Done: ${OUT_DIR}"
