#!/usr/bin/env bash
set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
EMAIL="${SMOKE_TEST_EMAIL:-}"
PASSWORD="${SMOKE_TEST_PASSWORD:-}"
SMOKE_RUN_MIGRATE="${SMOKE_RUN_MIGRATE:-false}"
SMOKE_MIGRATE_CMD="${SMOKE_MIGRATE_CMD:-docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate}"

WARNINGS=()
FAILURES=()

if [[ "${SMOKE_RUN_MIGRATE}" == "true" ]]; then
  echo "[0/4] Migration job kontrolu..."
  if ! eval "${SMOKE_MIGRATE_CMD}"; then
    FAILURES+=("migration")
  fi
fi

echo "[1/4] Swagger kontrolu..."
SWAGGER_STATUS="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/swagger" || true)"
if [[ "$SWAGGER_STATUS" != "200" ]]; then
  WARNINGS+=("Swagger kontrolu warning. status=${SWAGGER_STATUS}")
fi

echo "[2/4] Health kontrolu..."
HEALTH_STATUS="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health" || true)"
if [[ "$HEALTH_STATUS" != "200" ]]; then
  FAILURES+=("Health kontrolu basarisiz. status=${HEALTH_STATUS}")
fi

echo "[3/4] Auth login kontrolu..."
if [[ -z "$EMAIL" || -z "$PASSWORD" ]]; then
  FAILURES+=("SMOKE_TEST_EMAIL ve SMOKE_TEST_PASSWORD zorunlu. Auth/jobs adimlari atlanamaz.")
else
  LOGIN_RESPONSE="$(curl -sS -X POST "${BASE_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}")"

  TOKEN="$(printf '%s' "${LOGIN_RESPONSE}" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')"
  if [[ -z "$TOKEN" ]]; then
    FAILURES+=("Login basarisiz: accessToken alinamadi.")
  fi

  if [[ -n "$TOKEN" ]]; then
    echo "[4/4] Jobs endpoint kontrolu..."
    JOBS_STATUS="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/jobs" \
      -H "Authorization: Bearer ${TOKEN}" || true)"
    if [[ "$JOBS_STATUS" != "200" ]]; then
      FAILURES+=("Jobs kontrolu basarisiz. status=${JOBS_STATUS}")
    fi
  fi
fi

if [[ ${#FAILURES[@]} -gt 0 ]]; then
  echo "Smoke test FAIL"
  for item in "${FAILURES[@]}"; do
    echo "- ${item}"
  done
  for item in "${WARNINGS[@]}"; do
    echo "- WARN: ${item}"
  done
  exit 1
elif [[ ${#WARNINGS[@]} -gt 0 ]]; then
  echo "Smoke test WARN"
  for item in "${WARNINGS[@]}"; do
    echo "- ${item}"
  done
else
  echo "Smoke test PASS"
fi
