#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
REPORTS_DIR="$ROOT_DIR/docs/reports"

if [[ ! -d "$REPORTS_DIR" ]]; then
  echo "Reports directory not found: $REPORTS_DIR"
  exit 1
fi

if ! command -v pandoc >/dev/null 2>&1; then
  echo "SKIP (pandoc not found)"
  echo "Manual: install pandoc and run this script again."
  exit 0
fi

pandoc "$REPORTS_DIR/final-report.md" -o "$REPORTS_DIR/final-report.pdf"
pandoc "$REPORTS_DIR/slides-outline.md" -o "$REPORTS_DIR/slides-outline.pdf"
pandoc "$REPORTS_DIR/evaluation-matrix.md" -o "$REPORTS_DIR/evaluation-matrix.pdf"

echo "Reports PDF generated in $REPORTS_DIR"
