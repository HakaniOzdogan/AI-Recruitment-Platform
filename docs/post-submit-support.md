# Post-Submit Support

Teslim sonrasi hizli destek icin tek sayfa operasyon notu.

## 1) 30 Saniye Hizli Toparlama
```bash
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d db
docker compose -f docker-compose.prod.yml run --rm migrate
docker compose -f docker-compose.prod.yml up -d api web
```

## 2) Sik Gorulen 5 Sorun

1. `JWT_SECRET is required`
- Neden: `.env.prod` icinde `JWT_SECRET` bos/eksik.
- Cozum:
- `.env.prod` kontrol et.
- En az 32 karakter guclu deger set et.
- Servisleri yeniden baslat.

2. `Pending migrations`
- Neden: DB schema kodla uyumsuz.
- Cozum:
```bash
docker compose -f docker-compose.prod.yml run --rm migrate
```

3. `CORS` / API'ye ulasamama (dev)
- Neden: yanlis base URL veya proxy disi cagri.
- Cozum:
- Dev'de `VITE_API_BASE_URL=/api` kullan.
- Vite proxy target `http://localhost:8080` oldugunu kontrol et.

4. `Adaptive mode 409`
- Neden: LLM kapali veya adaptive flag kapali.
- Cozum:
- Beklenen davranistir (demo failure-case olarak da gosterilebilir).
- LLM gerekiyorsa ilgili env flag'lerini ac.

5. `File upload rejected`
- Neden: tip/size/magic-bytes uyumsuz.
- Cozum:
- `sample-data` dosyalarini kullan.
- Imza dogrulama:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/verify-sample-files.ps1`
- Linux/Mac: `bash ops/verify-sample-files.sh`

## 3) Demo Tekrarı Icin DB Reset
Dikkat: volume siler, tum lokal veri sifirlanir.

```bash
docker compose -f docker-compose.prod.yml down -v
```

Ardindan hizli kurulum icin:
```bash
bash ops/run-demo.sh
```
veya
```powershell
powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1
```

## 4) Log Toplama

Son loglar:
```bash
docker compose -f docker-compose.prod.yml logs api --tail=200
```

CorrelationId ile eslestirme:
- Linux/Mac:
```bash
docker compose -f docker-compose.prod.yml logs api | grep "<correlation-id>"
```
- PowerShell:
```powershell
docker compose -f docker-compose.prod.yml logs api | Select-String "<correlation-id>"
```
