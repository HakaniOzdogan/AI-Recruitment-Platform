# Troubleshooting

## 1) JWT_SECRET missing
Belirti:
- API startup'ta kapanir.

Cozum:
- `.env.prod` icine guclu bir `JWT_SECRET` gir (minimum 32 karakter).
- Sonra: `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d api`

## 2) Pending migration / schema uyumsuzlugu
Belirti:
- API'de eksik kolon/tablo hatalari.

Cozum:
- `docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm migrate`
- Ardindan `api` servisini restart et.

## 2.1 `.env.prod` eksik
Belirti:
- Compose komutlari eksik/env bos degerlerle calisir veya servisler ayaga kalkmaz.

Cozum:
- Linux/Mac: `cp .env.prod.example .env.prod`
- PowerShell: `Copy-Item .env.prod.example .env.prod`
- Sonra `.env.prod` icini doldurup compose komutlarini `--env-file .env.prod` ile calistir.

## 3) CORS veya API erisim sorunu
Belirti:
- Browser'da CORS veya network hatalari.

Cozum:
- Onerilen: `WEB_API_BASE_URL=/api` kullan (same-origin reverse proxy).
- Alternatif: backend CORS allowlist'i dogru origin ile guncelle.

## 4) LLM hatasi / adaptive acilmiyor
Belirti:
- ADAPTIVE mode 409 donuyor.
- AI evaluate veya enrichment fail ediyor.

Cozum:
- `LLM_ENABLED`, `ADAPTIVE_INTERVIEW_ENABLED`, `LLM_SCORING_ENABLED` flaglerini kontrol et.
- LLM sorununda gecici fallback: `LLM_ENABLED=false`.

## 5) 403 / 409 / 429 sik goruluyor
- `403`: Yetki eksik. Admin veya uygun permission ile test et.
- `409`: Consent/state conflict. Is akisini kontrol et.
- `429`: Rate limit. Istek frekansini azalt veya policy degerlerini dogrula.

## 6) Testlerde CS2012 lock
Belirti:
- testhost DLL lock hatasi.

Cozum:
- `dotnet test --no-build` kullan.
- Paralel testi kapali kos.

## 7) CorrelationId ile hata takibi
- UI'da hata mesajinda `Ref: <correlationId>` gorulur.
- Backend loglarda ayni id ile ilgili request'i filtreleyip root cause bulunur.
