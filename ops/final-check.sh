#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
WARN=0

echo "[1/5] Port check"
check_url() {
  local url="$1"
  local name="$2"
  local code
  code="$(curl -s -o /dev/null -w "%{http_code}" "$url" || true)"
  if [[ "$code" == "200" || "$code" == "301" || "$code" == "302" ]]; then
    echo "OK: $name ($url)"
  else
    echo "WARN: $name not ready ($url), code=$code"
    WARN=1
  fi
}
check_url "http://localhost:3000" "web"
check_url "http://localhost:8080/swagger" "api"

echo "[2/5] Required files"
for f in VERSION CHANGELOG.md docs/acceptance-signoff.md docs/risk-register.md docs/demo-10min-cheatsheet.md; do
  if [[ -f "$ROOT_DIR/$f" ]]; then
    echo "OK: $f"
  else
    echo "WARN: missing $f"
    WARN=1
  fi
done

echo "[3/5] Submission artifacts"
if ls "$ROOT_DIR"/ik-otomasyon-submission-v*.zip >/dev/null 2>&1; then
  echo "OK: submission zip exists"
else
  echo "WARN: submission zip missing"
  WARN=1
fi
if [[ -f "$ROOT_DIR/submission/MANIFEST.txt" ]]; then
  echo "OK: submission/MANIFEST.txt exists"
else
  echo "WARN: submission/MANIFEST.txt missing"
  WARN=1
fi

echo "[4/5] Evidence latest"
if [[ -d "$ROOT_DIR/evidence" ]] && find "$ROOT_DIR/evidence" -mindepth 1 -maxdepth 1 -type d | read -r; then
  LATEST="$(find "$ROOT_DIR/evidence" -mindepth 1 -maxdepth 1 -type d | sort | tail -n 1)"
  echo "OK: evidence dir -> $LATEST"
else
  echo "WARN: no evidence run found"
  WARN=1
fi

echo "[5/5] Secret/PII scan"
if rg -n "BEGIN PRIVATE KEY|BEGIN RSA PRIVATE KEY" "$ROOT_DIR" \
  --glob '!backend/bin/**' --glob '!backend/obj/**' --glob '!frontend/node_modules/**' \
  --glob '!evidence/**' --glob '!submission/**' --glob '!ops/**' >/dev/null; then
  echo "WARN: private key pattern found"
  WARN=1
else
  echo "OK: no private key pattern"
fi

if rg -n "JWT_SECRET=" "$ROOT_DIR" \
  --glob '!.env*' --glob '!*.example' --glob '!docs/**' --glob '!submission/**' --glob '!evidence/**' --glob '!ops/**' >/dev/null; then
  echo "WARN: JWT_SECRET assignment found in tracked files"
  WARN=1
else
  echo "OK: no JWT_SECRET assignment in tracked source/docs"
fi

if rg -n "Demo Candidate|demo@local\\.test" "$ROOT_DIR/sample-data" >/dev/null; then
  echo "OK: sample-data appears anonymized"
else
  echo "WARN: sample-data markers not found"
  WARN=1
fi

if [[ "$WARN" -eq 0 ]]; then
  echo "FINAL CHECK: OK"
else
  echo "FINAL CHECK: WARN"
fi
