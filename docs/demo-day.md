# Demo Day (Quick Recovery Sheet)

Bu sayfa demo gunu icin tek sayfalik hizli referanstir.

## 1) Baslangic
Windows:
```powershell
powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1
```

Linux/Mac:
```bash
bash ops/run-demo.sh
```

- Web: `http://localhost:3000`
- API: `http://localhost:8080`

## 2) Acilacak Sekmeler
1. `http://localhost:3000/jobs`
2. `http://localhost:3000/candidates`
3. `http://localhost:3000/jobs/{id}`
4. `http://localhost:3000/interviews/{id}`

## 3) 60 Saniyelik Toparlama
```bash
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d db
docker compose -f docker-compose.prod.yml run --rm migrate
docker compose -f docker-compose.prod.yml up -d api web
```

## 4) En Sik 5 Hata ve Hemen Cozum
1. `JWT_SECRET is required`
- `.env.prod` icinde `JWT_SECRET` degeri var mi kontrol et.
- Gerekirse `.env.prod.example` kopyalayip tekrar dene.

2. `Pending migrations`
- Migrate calistir:
```bash
docker compose -f docker-compose.prod.yml run --rm migrate
```

3. CORS / API baglanti sorunu
- Dev modda `VITE_API_BASE_URL=/api` ve Vite proxy kullan.
- Gerekirse frontend'i yeniden baslat.

4. Adaptive mode `409`
- Beklenen durum olabilir: LLM kapaliysa ADAPTIVE acilmaz.
- Demo'da bu davranisi "guardrail/fallback" olarak goster.

5. File upload rejected
- Sample dosya imzasini dogrula:
  - Windows: `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
  - Linux/Mac: `bash ops/verify-sample-files.sh`

## 5) Kapanis Hatirlatma
- Demo sonunda:
  - `evidence/YYYYMMDD-HHMM/` klasorunu goster
  - `ik-otomasyon-submission-v1.0.0.zip` + `submission/MANIFEST.txt` dosyalarini goster
