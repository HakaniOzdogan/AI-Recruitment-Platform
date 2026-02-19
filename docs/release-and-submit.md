# Release And Submit

Bu dokuman, teslim oncesi son adimlari tek sayfada toplar.

## 1) Release Tag (manual)
```bash
git checkout main
git pull
git status
```

Not:
- `git status` temiz olmali (clean).

Tag olustur:
```bash
git tag v1.0.0
git push origin v1.0.0
```

## 2) Teslim Paketi
Zorunlu dosyalar:
- `ik-otomasyon-submission-v1.0.0.zip`
- `submission/MANIFEST.txt`

Ek kanit (onerilen):
- `evidence/YYYYMMDD-HHMM/` klasoru
- Istersen evidence'i ayri zip olarak da teslim et.

## 3) Demo Dokumanlari
- `docs/demo-10min-cheatsheet.md`
- `docs/demo-open-tabs.md`

## 4) Calistirma (run-demo)
- Windows:
```powershell
powershell -ExecutionPolicy Bypass -File ops/run-demo.ps1
```
- Linux/Mac:
```bash
bash ops/run-demo.sh
```

## 5) Teslim Notlari
- `.env.prod` icinde `JWT_SECRET` zorunludur.
- LLM kapaliysa ADAPTIVE mode degisiminde `409` beklenir (demo failure-case olarak gosterilebilir).

## 6) Opsiyonel Kisa Teslim Mesaji
```text
Merhaba,

Proje teslim paketi ektedir:
- ik-otomasyon-submission-v1.0.0.zip
- submission/MANIFEST.txt
- (opsiyonel) evidence klasoru

Demo akisi:
- docs/demo-10min-cheatsheet.md
- docs/demo-open-tabs.md

Calistirma:
- ops/run-demo.ps1 (Windows)
- ops/run-demo.sh (Linux/Mac)

Iyi calismalar.
```
