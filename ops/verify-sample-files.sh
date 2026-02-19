#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PDF_FILE="${ROOT_DIR}/sample-data/sample-candidate.pdf"
DOCX_FILE="${ROOT_DIR}/sample-data/sample-candidate.docx"

if [[ ! -f "${PDF_FILE}" ]]; then
  echo "Eksik dosya: ${PDF_FILE}"
  exit 1
fi
if [[ ! -f "${DOCX_FILE}" ]]; then
  echo "Eksik dosya: ${DOCX_FILE}"
  exit 1
fi

PDF_SIG="$(head -c 5 "${PDF_FILE}" || true)"
DOCX_SIG="$(head -c 2 "${DOCX_FILE}" || true)"

if [[ "${PDF_SIG}" != "%PDF-" ]]; then
  echo "PDF signature fail. Beklenen: %PDF-"
  exit 1
fi
if [[ "${DOCX_SIG}" != "PK" ]]; then
  echo "DOCX signature fail. Beklenen: PK"
  exit 1
fi

echo "PASS: sample PDF/DOCX signatures are valid."
