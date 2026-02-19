# Final Send (2-Min Checklist)

Teslimden hemen once bu listeyi 2 dakikada kontrol et.

## 1) Deliverables klasoru
- [ ] `deliverables/ik-otomasyon-submission-v1.0.0.zip`
- [ ] `deliverables/MANIFEST.txt`
- [ ] `deliverables/evidence-*.zip` (varsa)
- [ ] `deliverables/ONE-PAGER.md`
- [ ] `deliverables/demo-10min-cheatsheet.md`
- [ ] `deliverables/demo-seed-5min.md`
- [ ] `deliverables/submission-notes.md`
- [ ] `deliverables/delivery-message.md`

## 2) Hizli teknik kontrol
- [ ] Submission zip aciliyor mu?
- [ ] Dokumanlarda run-demo komutu var mi?
- [ ] Evidence zip mevcut mu?
- [ ] `docs/acceptance-signoff.md` PASS mi?
- [ ] Secrets/PII iceren dosya yok mu?

## 3) Hash listesi (opsiyonel ama onerilir)
Windows:
```powershell
Get-FileHash deliverables\* -Algorithm SHA256 > deliverables\DELIVERABLES_SHA256.txt
```

Linux/Mac:
```bash
sha256sum deliverables/* > deliverables/DELIVERABLES_SHA256.txt
```

## 4) Gonderim paketi
- `deliverables/` klasorunu aynen paylas.
- Kisa mesaj icin: `docs/delivery-message.md`.
