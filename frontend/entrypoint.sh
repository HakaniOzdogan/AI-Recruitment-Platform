#!/bin/sh
set -eu

RUNTIME_API_BASE_URL="${API_BASE_URL:-/api}"

cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = {
  API_BASE_URL: "${RUNTIME_API_BASE_URL}"
};
EOF

exec nginx -g 'daemon off;'