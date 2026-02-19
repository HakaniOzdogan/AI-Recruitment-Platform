# Frontend Dev Run

## 1) Backend baslat
- Backend API `http://localhost:8080` adresinde calismali.
- Ornek: backend dizininde `dotnet run`.

## 2) Frontend env
- `frontend/.env.example` dosyasini `frontend/.env` olarak kopyala.
- Varsayilan dev ayari:
- `VITE_API_BASE_URL=/api`
- `VITE_API_PROXY_TARGET=http://localhost:8080`

## 3) Frontend baslat
```bash
cd frontend
npm i
npm run dev
```

Beklenen:
- Tarayici `http://localhost:5173` (veya vite'in verdigi port) acar.
- Network istekleri `/api/...` olarak gider.
- Vite proxy bunlari `http://localhost:8080`'e yonlendirir (CORS sorunu olmaz).

## 4) Login ve ilk rota
- Login basariliysa varsayilan rota: `/jobs`.
- Protected sayfaya dogrudan girildiyse login sonrasi o sayfaya geri doner.

## 5) Mini baglanti testi
1. Login olmadan `/jobs` ac -> `/login`'e yonlendirme olur.
2. Login ol -> `/jobs` acilir.
3. Jobs listesi doluysa tablo, bossa empty state + CTA gorunur.

## Common Issues
- `404` (API)
: `VITE_API_BASE_URL` yanlis olabilir. Dev'de `/api` kullan.
- `CORS` hatasi
: Vite proxy devrede degil. `vite.config.js` ve `.env` kontrol et.
- `401` surekli
: token yok/suresi dolmus. Tekrar login ol.
