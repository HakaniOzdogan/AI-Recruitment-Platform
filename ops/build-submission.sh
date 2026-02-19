#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
SUB_DIR="${ROOT_DIR}/submission"
VERSION="$(cat "${ROOT_DIR}/VERSION" | tr -d ' \r\n')"
ZIP_NAME="ik-otomasyon-submission-v${VERSION}.zip"
ZIP_PATH="${ROOT_DIR}/${ZIP_NAME}"

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || return 1
}

hash_cmd() {
  if require_cmd sha256sum; then
    echo "sha256sum"
  elif require_cmd shasum; then
    echo "shasum -a 256"
  else
    echo ""
  fi
}

echo "[1/6] Clean submission directory"
rm -rf "${SUB_DIR}"
mkdir -p "${SUB_DIR}"

echo "[2/6] Copy repository content (filtered)"
if require_cmd rsync; then
  rsync -a \
    --exclude 'node_modules' \
    --exclude 'dist' \
    --exclude 'bin' \
    --exclude 'obj' \
    --exclude 'evidence' \
    --exclude '.git' \
    --exclude '.github' \
    --exclude '.env*' \
    --exclude '*.user' \
    --exclude '*.suo' \
    --exclude 'submission' \
    --exclude '*.zip' \
    "${ROOT_DIR}/backend" "${SUB_DIR}/"
  rsync -a \
    --exclude 'node_modules' \
    --exclude 'dist' \
    --exclude '.env*' \
    "${ROOT_DIR}/frontend" "${SUB_DIR}/"
  rsync -a "${ROOT_DIR}/docs" "${SUB_DIR}/"
  rsync -a "${ROOT_DIR}/ops" "${SUB_DIR}/"
else
  echo "WARN: rsync bulunamadi. cp fallback kullaniliyor."
  cp -R "${ROOT_DIR}/backend" "${SUB_DIR}/"
  cp -R "${ROOT_DIR}/frontend" "${SUB_DIR}/"
  cp -R "${ROOT_DIR}/docs" "${SUB_DIR}/"
  cp -R "${ROOT_DIR}/ops" "${SUB_DIR}/"
  find "${SUB_DIR}" -type d \( -name node_modules -o -name dist -o -name bin -o -name obj -o -name evidence \) -prune -exec rm -rf {} +
fi

for f in README.md CHANGELOG.md VERSION docker-compose.yml docker-compose.prod.yml; do
  if [[ -f "${ROOT_DIR}/${f}" ]]; then
    cp "${ROOT_DIR}/${f}" "${SUB_DIR}/"
  fi
done

for f in "${ROOT_DIR}/backend/Dockerfile" "${ROOT_DIR}/frontend/Dockerfile"; do
  if [[ -f "$f" ]]; then
    mkdir -p "${SUB_DIR}/$(dirname "${f#${ROOT_DIR}/}")"
    cp "$f" "${SUB_DIR}/$(dirname "${f#${ROOT_DIR}/}")/"
  fi
done

echo "[3/6] Optional PDF generation"
if require_cmd pandoc; then
  pandoc "${SUB_DIR}/docs/final-report.md" -o "${SUB_DIR}/docs/final-report.pdf" || true
  pandoc "${SUB_DIR}/docs/slides-outline.md" -o "${SUB_DIR}/docs/slides-outline.pdf" || true
  echo "pandoc: PDF generation attempted." > "${SUB_DIR}/docs/pdf-status.txt"
else
  echo "SKIP (pandoc not found)" > "${SUB_DIR}/docs/pdf-status.txt"
fi

echo "[4/6] Build MANIFEST"
MANIFEST="${SUB_DIR}/MANIFEST.txt"
: > "${MANIFEST}"
HASHER="$(hash_cmd)"
if [[ -z "${HASHER}" ]]; then
  echo "WARN: sha256 araci bulunamadi. hash bolumu atlandi." >> "${MANIFEST}"
else
  (
    cd "${SUB_DIR}"
    find . -type f ! -name "MANIFEST.txt" -print0 | sort -z | while IFS= read -r -d '' file; do
      rel="${file#./}"
      if [[ "${HASHER}" == "sha256sum" ]]; then
        hash="$(sha256sum "$rel" | awk '{print $1}')"
      else
        hash="$(shasum -a 256 "$rel" | awk '{print $1}')"
      fi
      printf "%s  %s\n" "$hash" "$rel" >> "MANIFEST.txt"
    done
  )
fi

echo "[5/6] Create zip"
rm -f "${ZIP_PATH}"
if require_cmd zip; then
  (
    cd "${ROOT_DIR}"
    zip -r "${ZIP_NAME}" submission >/dev/null
  )
else
  if require_cmd tar; then
    (
      cd "${ROOT_DIR}"
      tar -a -cf "${ZIP_NAME}" submission
    )
  else
    echo "zip/tar bulunamadi, zip olusturulamadi."
    exit 1
  fi
fi

echo "[6/6] Done"
echo "Output: ${ZIP_PATH}"
