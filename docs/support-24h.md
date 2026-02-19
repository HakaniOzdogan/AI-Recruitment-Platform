# Support Plan (First 24 Hours)

Bu dokuman teslim sonrasi ilk 24 saatte hizli destek icin kullanilir.

## Hizli Triage (ilk mesajda iste)
1. Isletim sistemi + Docker/Compose versiyonlari
2. API loglari:
   - `docker compose -f docker-compose.prod.yml logs api --tail=200`
3. UI hata referansi:
   - ekranda gorunen `Ref: <correlationId>`

## 60 Saniye Restart
```bash
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d db
docker compose -f docker-compose.prod.yml run --rm migrate
docker compose -f docker-compose.prod.yml up -d api web
```

## En Sik Sorunlar ve Hizli Cozum
1. `JWT_SECRET is required`
- `.env.prod` icinde `JWT_SECRET` dolu olmali (min 32 karakter).
- Sonra `migrate` ve `api` tekrar calistirilir.

2. Pending migrations / schema uyumsuzlugu
- `docker compose -f docker-compose.prod.yml run --rm migrate`
- Sonra `api` yeniden baslat.

3. Port cakismasi (3000/8080)
- `docker ps` ile kullanan container/process kontrol et.
- Gerekirse compose port mapping degistir veya cakan servisi kapat.

4. CORS / API baglanamiyor (dev ortam)
- Frontend dev calisiyorsa `/api` proxy kullan.
- Production senaryoda web container uzerinden same-origin tercih et.

5. Adaptive mode 409
- Beklenen durum olabilir: `LLM_ENABLED` veya `ADAPTIVE_INTERVIEW_ENABLED` kapali.
- Demo icin deterministic akisla devam edilir.

## “Calismiyor” Derlerse Istenilecek 3 Bilgi
1. Hangi adimda hata oldugu + ekran goruntusu
2. UI `Ref: <correlationId>` degeri
3. Son 200 satir API logu (`docker compose ... logs api --tail=200`)

## Log Eslestirme Notu
- CorrelationId varsa:
  - `docker compose -f docker-compose.prod.yml logs api | grep <correlationId>`
- Windows PowerShell:
  - `docker compose -f docker-compose.prod.yml logs api | Select-String <correlationId>`
