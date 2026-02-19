#!/usr/bin/env bash
set -euo pipefail

SOLUTION_PATH="${SOLUTION_PATH:-İk_otomasyon.sln}"
API_PROJECT="${API_PROJECT:-backend/IkOtomasyon.Api.csproj}"
STARTUP_PROJECT="${STARTUP_PROJECT:-backend/IkOtomasyon.Api.csproj}"
RUN_DOCKER_BUILD="${RUN_DOCKER_BUILD:-false}"

echo "[1/6] Restore"
dotnet restore "${SOLUTION_PATH}"

echo "[2/6] Build"
dotnet build "${SOLUTION_PATH}" -c Release --no-restore

echo "[3/6] Test"
dotnet test "${SOLUTION_PATH}" -c Release --no-build

echo "[4/6] Migration check"
dotnet ef migrations list --project "${API_PROJECT}" --startup-project "${STARTUP_PROJECT}" > migration-gate.txt
if grep -q "(Pending)" migration-gate.txt; then
  echo "Pending migrations bulundu:"
  cat migration-gate.txt
  exit 1
fi

echo "[5/6] Vulnerability check"
dotnet list "${API_PROJECT}" package --vulnerable --include-transitive > vulnerability-gate.txt
if grep -Eiq 'High|Critical' vulnerability-gate.txt; then
  echo "High/Critical vulnerability bulundu:"
  cat vulnerability-gate.txt
  exit 1
fi

if [[ "${RUN_DOCKER_BUILD}" == "true" ]]; then
  echo "[6/6] Docker build"
  docker build -t ik-otomasyon-api:release ./backend
else
  echo "[6/6] Docker build skip (RUN_DOCKER_BUILD=false)"
fi

echo "READY"
