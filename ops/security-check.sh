#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
ENVIRONMENT="${ENVIRONMENT:-Production}"
JWT_SECRET_VALUE="${JWT_SECRET:-}"

echo "[1/4] Security headers kontrolu"
HEADERS="$(curl -sSI "${BASE_URL}/health" || true)"

if [[ "${ENVIRONMENT}" == "Production" ]]; then
  echo "${HEADERS}" | grep -qi "strict-transport-security" || { echo "HSTS missing"; exit 1; }
fi

echo "${HEADERS}" | grep -qi "x-content-type-options: nosniff" || { echo "nosniff missing"; exit 1; }
echo "${HEADERS}" | grep -qi "x-frame-options: deny" || { echo "x-frame-options missing"; exit 1; }
echo "${HEADERS}" | grep -qi "referrer-policy" || { echo "referrer-policy missing"; exit 1; }

echo "[2/4] CORS allowlist davranisi kontrolu"
CORS_HEADERS="$(curl -sSI -H "Origin: https://evil.example.com" "${BASE_URL}/health" || true)"
if [[ "${ENVIRONMENT}" == "Production" ]]; then
  if echo "${CORS_HEADERS}" | grep -qi "access-control-allow-origin: \\*"; then
    echo "Production ortaminda wildcard CORS tespit edildi."
    exit 1
  fi
fi

echo "[3/4] JWT secret uzunluk sanity check"
if [[ -z "${JWT_SECRET_VALUE}" ]]; then
  echo "JWT_SECRET env bulunamadi."
  exit 1
fi
if (( ${#JWT_SECRET_VALUE} < 32 )); then
  echo "JWT_SECRET minimum 32 karakter olmali."
  exit 1
fi

echo "[4/4] Health endpoint kontrolu"
STATUS="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health")"
if [[ "${STATUS}" != "200" ]]; then
  echo "Health check basarisiz. status=${STATUS}"
  exit 1
fi

echo "Security checks passed."
