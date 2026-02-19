#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="${1:-docker-compose.prod.yml}"

echo "[1/2] Stopping containers"
docker compose -f "$COMPOSE_FILE" down

echo "[2/2] Optional full reset (volumes)"
echo "Run manually if needed:"
echo "docker compose -f $COMPOSE_FILE down -v"

echo "Cleanup done."
