# Frontend (Demo UI)

## Local Dev
- `cd frontend`
- `npm install`
- `npm run dev`

Varsayilan API URL:
- `VITE_API_BASE_URL=http://localhost:8080`

`.env.example` kopyasi ile override edebilirsin.

## Production Build
- `npm run build`
- output: `frontend/dist`

## Docker Build
- `docker build -t ik-web ./frontend`

## Docker Run
- Reverse proxy same-origin (onerilen):
  - `docker run --rm -p 3000:80 -e API_BASE_URL=/api ik-web`
- Dogrudan backend URL:
  - `docker run --rm -p 3000:80 -e API_BASE_URL=http://localhost:8080 ik-web`

## Runtime Config (Build-Time degil)
Container acilisinda `entrypoint.sh` dosyasi `/usr/share/nginx/html/config.js` uretir:
- `window.__APP_CONFIG__.API_BASE_URL`

`src/config.ts` sirasi:
1. `window.__APP_CONFIG__.API_BASE_URL`
2. `import.meta.env.VITE_API_BASE_URL`
3. `http://localhost:8080`

## Nginx Davranisi
- SPA routing: `try_files $uri /index.html`
- Cache:
  - `index.html` ve `config.js`: no-cache
  - `/assets/*`: 1 yil immutable
- Security headers:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy`
- Opsiyonel reverse proxy:
  - `/api/* -> http://api:8080/*`
