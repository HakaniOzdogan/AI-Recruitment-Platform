#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
WARN=0

echo "[1/4] Secret pattern scan"
if rg -n "BEGIN PRIVATE KEY|BEGIN RSA PRIVATE KEY|AKIA[0-9A-Z]{16}" \
  "$ROOT_DIR" \
  --glob "!backend/bin/**" \
  --glob "!backend/obj/**" \
  --glob "!frontend/node_modules/**" \
  --glob "!evidence/**" \
  --glob "!ops/pre-freeze-check.sh" \
  --glob "!ops/pre-freeze-check.ps1"; then
  echo "WARN: Potential secret material detected."
  WARN=1
else
  echo "OK: No private key / obvious token patterns found."
fi

echo "[2/4] .env tracked check"
if [[ -f "$ROOT_DIR/.env.prod" ]]; then
  echo "WARN: .env.prod file exists locally. Ensure it is not committed."
  WARN=1
else
  echo "OK: .env.prod not present in repo root."
fi

echo "[3/4] Sample-data PII sanity"
if rg -n "Demo Candidate|demo@local\\.test" "$ROOT_DIR/sample-data" >/dev/null; then
  echo "OK: sample-data contains demo identity markers."
else
  echo "WARN: sample-data does not contain expected demo identity markers."
  WARN=1
fi

echo "[4/4] Required metadata files"
for f in VERSION CHANGELOG.md docs/acceptance-signoff.md docs/risk-register.md docs/freeze.md; do
  if [[ ! -f "$ROOT_DIR/$f" ]]; then
    echo "WARN: missing $f"
    WARN=1
  fi
done

if [[ "$WARN" -eq 0 ]]; then
  echo "Pre-freeze check: PASS"
else
  echo "Pre-freeze check: WARNINGS_FOUND"
fi
