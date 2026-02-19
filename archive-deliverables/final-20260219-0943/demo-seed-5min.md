# Demo Seed 5 Dakika

Bu akis bos DB'de hizli demo verisi olusturmak icin UI odakli minimum adim listesidir.

1. Login
- `admin@local.test / Admin123!` ile giris yap.

2. Job olustur
- `Jobs` sayfasinda `Create Job`.
- En az `title` + `description` doldur.
- Kaydet, listede satirin geldigini dogrula.

3. Job publish
- Ayni satirdan `Publish`.
- Status `Published` oldugunu dogrula.

4. Candidate olustur
- `Candidates` sayfasinda `Create Candidate`.
- `fullName` zorunlu alanini doldur ve kaydet.

5. CV upload
- Candidate detail sayfasina gir.
- `sample-data/sample-candidate.pdf` veya `sample-data/sample-candidate.docx` yukle.

6. Parse + profile
- `Parse CV` tikla.
- `Candidate Profile` altinda skills/experience bilgisinin geldigini dogrula.

7. Application bagla
- Job detail -> `Applications` paneli.
- `Add candidate to job` ile adayi ilana bagla.

8. Interview baslat
- Applications satirinda `Start Interview`.
- Interview sayfasina yonlendirmeyi dogrula.

9. Mesaj gonder
- Interview chat input'una mesaj yaz, `Send`.
- Mesajin listede gorundugunu dogrula.

10. Auto score
- `Scorecard` panelinde `Run Auto Score`.
- En az bir criterion satirinin geldigini dogrula.
