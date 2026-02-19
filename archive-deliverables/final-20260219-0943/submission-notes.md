# Submission Notes

## Version
- VERSION: `1.0.0`
- Release tag (manual): `v1.0.0`

## Delivered Files
- `ik-otomasyon-submission-v1.0.0.zip`
- `submission/MANIFEST.txt`

## Kurulum / Calistirma
Windows:
```powershell
powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1
```

Linux/Mac:
```bash
bash ops/run-demo.sh
```

## Demo
- 10 dk akis: `docs/demo-10min-cheatsheet.md`
- 5 dk seed: `docs/demo-seed-5min.md`

## Kanit
- Ana kanit paketi: `docs/evidence-pack.md`
- Correlation kaniti: `docs/correlation-proof.md`
- Uretilen kanitlar: `evidence/YYYYMMDD-HHMM/`

## Notlar
- Bundle icinde secret yok (`.env*` disarida).
- `run-demo` oncesi gerekirse `.env.prod.example` -> `.env.prod` kopyalanmali.
