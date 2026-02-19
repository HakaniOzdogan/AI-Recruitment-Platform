# Demo Script (10 Dakika)

## Hazirlik (30-60 sn)
- `bash ops/demo-setup.sh`
- Web: `http://localhost:3000`
- API Swagger: `http://localhost:8080/swagger`
- Login: `.env.prod` icindeki demo kullanici bilgileri

## 10 Dakikalik Akis
1. Login: admin kullanicisi ile giris yap.
2. Job create + publish: yeni ilan ac, `Publish` et.
3. Weights: competency toplam `1.0`, skill weights kaydet.
4. Candidate + CV: aday olustur, CV yukle, parse et, profile gor.
5. Matching: job detail'de shortlist ac, reasons/gaps goster.
6. Application: adayi ilana bagla, stage degistir.
7. Interview: session baslat, chat mesaji gonder.
8. Adaptive denemesi: OFF -> ADAPTIVE; kapaliysa 409 mesajini goster.
9. Scorecard: auto score calistir, `INSUFFICIENT_EVIDENCE` etiketini goster, human override yap.
10. AI Evaluation: latest raporu cek, gerekirse `force=true` ile tekrar uret.

## 3 Dakikalik Teknik Ozet (sunum sonu)
- Uc ana omurga: RBAC + ATS + AI katmanlari.
- Deterministic temel akis her zaman calisir; LLM katmani guardrail + fallback ile kontrollu.
- Audit log + correlation id ile operasyonel izlenebilirlik saglanir.

## Demo Sirasinda Olasi Hatalar
- `403 weights`: kullanicida `JOB_CREATE` yetkisi yok.
- `409 consent required`: AI evaluation icin aday rizasi eksik.
- `409 adaptive disabled`: `LLM_ENABLED` veya `ADAPTIVE_INTERVIEW_ENABLED` kapali.
- `429 rate limit`: kisa surede cok istek atildi.

## Hizli Cozum
- Admin kullanici ile yeniden login ol.
- `.env.prod` flaglerini kontrol et.
- Gerekirse `docker compose -f docker-compose.prod.yml --env-file .env.prod restart api web`.
- Gerekirse `bash ops/smoke-test.sh` ile temel saglik dogrulasi yap.