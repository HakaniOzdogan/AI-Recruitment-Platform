# ONE-PAGER

## Proje Amaci
AI destekli IK otomasyonunda ilan -> aday -> basvuru -> mulakat -> degerlendirme akisini tek sistemde calisan, izlenebilir ve demo edilebilir hale getirmek.

## Moduller
- Jobs: ilan olusturma, yayinlama, agirlik/weights yonetimi.
- Candidates: aday olusturma, profil goruntuleme.
- CV Pipeline: upload, parse, profile extraction.
- Matching: job-candidate uyum skoru, reasons/gaps.
- Interview: chat tabanli oturum, OFF/ASSIST/ADAPTIVE mode.
- Scoring: auto score, insufficient evidence, human override.
- Audit: mutating islemler ve kritik AI olaylari icin kayit.

## Teknik Mimari
- API: .NET 8 Web API (JWT, RBAC, EF Core).
- DB: PostgreSQL.
- Web: React + TypeScript + Vite (nginx ile serve).
- LLM: provider-agnostic client + deterministic fallback.

## Guvenlik ve Kalite
- RBAC + permission bazli authorization.
- Secrets env tabanli (`JWT_SECRET` zorunlu).
- Vulnerability scan ve CI gate.
- Audit log + correlationId ile izlenebilirlik.
- Smoke/evidence scripts + submission manifest.

## Nasil Calistirilir
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

## Demo Akisi
- `docs/demo-10min-cheatsheet.md`
- `docs/demo-seed-5min.md`

## Kanit Paketi
- `docs/evidence-pack.md`
- `docs/correlation-proof.md`
- Uretilen cikti: `evidence/YYYYMMDD-HHMM/`

## Teslim Paketi
- Zip: `ik-otomasyon-submission-v1.0.0.zip`
- Manifest: `submission/MANIFEST.txt`
- Teslim notu: `docs/submission-notes.md`
