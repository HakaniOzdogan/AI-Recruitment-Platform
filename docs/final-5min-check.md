# Final 5-Minute Check

Teslimden hemen once bu listeyi hizlica gec.

- [ ] `run-demo` ile servisler ayakta:
- [ ] Web: `http://localhost:3000`
- [ ] API: `http://localhost:8080/swagger` (veya `/health`)
- [ ] Login calisiyor (`/login` -> `/jobs`).
- [ ] `docs/demo-10min-cheatsheet.md` acik ve guncel.
- [ ] Submission hazir:
- [ ] `ik-otomasyon-submission-v1.0.0.zip`
- [ ] `submission/MANIFEST.txt`
- [ ] Evidence klasoru var: `evidence/YYYYMMDD-HHMM/`
- [ ] Icinde: `swagger.json` (API up ise), `migrations.txt`, `vuln.txt`, `tests/*.trx`
- [ ] `docs/acceptance-signoff.md` PASS olarak doldurulmus.
- [ ] `docs/risk-register.md` guncel.
- [ ] Secrets/PII kontrolu:
- [ ] Repo'da `.env*` secret dosyasi yok (example disinda)
- [ ] Private key paterni yok
- [ ] `sample-data/` anonim (Demo Candidate)

## Hızlı Komutlar
- Windows:
- `powershell -ExecutionPolicy Bypass -File ops/final-check.ps1`
- Linux/Mac:
- `bash ops/final-check.sh`
