#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-.env.prod}"
WITH_WEB="${WITH_WEB:-true}"
RUN_SMOKE="${RUN_SMOKE:-false}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Env file bulunamadi: ${ENV_FILE}"
  echo "Ornekten olustur: cp .env.prod.example .env.prod"
  exit 1
fi

echo "[1/4] DB up"
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d db

echo "[2/4] Migrations"
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" run --rm migrate

echo "[3/4] API up"
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d api

if [[ "${WITH_WEB}" == "true" ]]; then
  echo "[4/4] Web up"
  docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d web
fi

if [[ "${RUN_SMOKE}" == "true" ]]; then
  echo "[SMOKE] API smoke test"
  BASE_URL="${BASE_URL:-http://localhost:8080}" \
  SMOKE_TEST_EMAIL="${SMOKE_TEST_EMAIL:-}" \
  SMOKE_TEST_PASSWORD="${SMOKE_TEST_PASSWORD:-}" \
  bash ops/smoke-test.sh
fi

echo ""
echo "Demo setup tamamlandi."
echo "API:  http://localhost:8080/swagger"
if [[ "${WITH_WEB}" == "true" ]]; then
  echo "WEB:  http://localhost:3000"
fi
if [[ -n "${SMOKE_TEST_EMAIL:-}" ]]; then
  echo "Login email: ${SMOKE_TEST_EMAIL}"
fi
if [[ -n "${SMOKE_TEST_PASSWORD:-}" ]]; then
  echo "Login password: ${SMOKE_TEST_PASSWORD}"
fi

echo "Not: Uretim benzeri ortamda otomatik admin seed yoksa uygun kullaniciyi DB seed ile olusturun."
