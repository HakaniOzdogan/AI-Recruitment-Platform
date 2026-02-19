#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-.env.prod}"
API_URL="${API_URL:-http://localhost:8080}"
WEB_URL="${WEB_URL:-http://localhost:3000}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-90}"
INTERVAL_SECONDS=3

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || { echo "Komut bulunamadi: $1"; exit 1; }
}

wait_http_ok() {
  local url="$1"
  local name="$2"
  local elapsed=0
  while (( elapsed < TIMEOUT_SECONDS )); do
    code="$(curl -s -o /dev/null -w "%{http_code}" "$url" || true)"
    if [[ "$code" == "200" ]]; then
      echo "Hazir: ${name} (${url})"
      return 0
    fi
    sleep "${INTERVAL_SECONDS}"
    elapsed=$((elapsed + INTERVAL_SECONDS))
  done
  echo "Timeout: ${name} hazir degil (${url})"
  return 1
}

echo "[0/7] Preconditions"
require_cmd docker
require_cmd curl
docker --version
docker compose version
if command -v dotnet >/dev/null 2>&1; then
  dotnet --version
else
  echo "Not: dotnet bulunamadi (compose calismasi icin zorunlu degil)."
fi

echo "[1/7] Env hazirligi"
cd "${ROOT_DIR}"
if [[ ! -f "${ENV_FILE}" ]]; then
  cp .env.prod.example "${ENV_FILE}"
  echo "${ENV_FILE} olusturuldu (.env.prod.example kopyasi)."
fi

JWT_LINE="$(grep -E '^JWT_SECRET=' "${ENV_FILE}" || true)"
JWT_VALUE="${JWT_LINE#JWT_SECRET=}"
if [[ -z "${JWT_VALUE}" || "${JWT_VALUE}" == "change_me_minimum_32_chars_secret_value" ]]; then
  if command -v openssl >/dev/null 2>&1; then
    GENERATED_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
  else
    GENERATED_SECRET="$(date +%s%N | sha256sum | awk '{print $1}')$(date +%s | sha256sum | awk '{print $1}')"
  fi
  sed -i.bak -E "s|^JWT_SECRET=.*|JWT_SECRET=${GENERATED_SECRET}|" "${ENV_FILE}" && rm -f "${ENV_FILE}.bak"
  JWT_VALUE="${GENERATED_SECRET}"
  echo "JWT_SECRET otomatik uretildi ve ${ENV_FILE} dosyasina yazildi."
fi

if [[ -z "${JWT_VALUE}" || ${#JWT_VALUE} -lt 32 || "${JWT_VALUE}" == "change_me_minimum_32_chars_secret_value" ]]; then
  echo "HATA: JWT_SECRET gecersiz. ${ENV_FILE} icinde en az 32 karakter guclu bir deger olmalidir."
  exit 1
fi

set -a
source "${ENV_FILE}"
set +a

echo "[2/7] Compose up (db -> migrate -> api+web)"
if [[ -z "${JWT_SECRET:-}" ]]; then
  echo "HATA: JWT_SECRET env degiskeni yuklenemedi. ${ENV_FILE} kontrol edin."
  exit 1
fi
if ! docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" down --remove-orphans; then
  echo "WARN: compose cleanup adimi basarisiz oldu, devam ediliyor."
fi
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d db
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" run --rm migrate
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d --build api web

echo "[3/7] Hazirlik kontrolu"
wait_http_ok "${API_URL}/health" "API health" || wait_http_ok "${API_URL}/swagger" "API swagger"
wait_http_ok "${WEB_URL}" "Web UI"

echo "[4/7] Smoke test"
SMOKE_STATUS=0
if [[ -z "${SMOKE_TEST_EMAIL:-}" || -z "${SMOKE_TEST_PASSWORD:-}" ]]; then
  echo "ERROR: SMOKE_TEST_EMAIL ve SMOKE_TEST_PASSWORD ortam degiskenleri zorunlu."
  SMOKE_STATUS=1
else
if ! bash ops/smoke-test.sh; then
  SMOKE_STATUS=1
  echo "ERROR: Smoke test failed."
fi
fi

echo "[5/7] Sample-data imza kontrolu"
head -c 5 sample-data/sample-candidate.pdf | grep -q '%PDF-' || { echo "PDF imzasi gecersiz"; exit 1; }
head -c 2 sample-data/sample-candidate.docx | grep -q 'PK' || { echo "DOCX imzasi gecersiz"; exit 1; }
echo "Sample-data imzalari OK"

echo "[6/7] Evidence snapshot"
EVIDENCE_STATUS=0
if ! bash ops/evidence.sh; then
  EVIDENCE_STATUS=1
  echo "WARN: Evidence toplama adimi failed."
fi

echo "[7/7] Hazir"
echo "NEXT: ${WEB_URL}"
echo "Open ${WEB_URL}"
echo "Next: follow docs/demo-seed-5min.md"

if [[ "${SMOKE_STATUS}" -ne 0 || "${EVIDENCE_STATUS}" -ne 0 ]]; then
  echo "Run-demo failed: smoke=${SMOKE_STATUS}, evidence=${EVIDENCE_STATUS}"
  exit 1
fi
