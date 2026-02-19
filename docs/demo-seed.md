# Demo Seed Rehberi

## 1) Login bilgisi
- Development seed aktifse:
- `admin@local.test / Admin123!`
- Production-benzeri compose ortaminda otomatik admin her zaman garanti degildir.
- Gerekirse once admin kullaniciyi mevcut seed mekanizmasi ile olusturun.

## 2) Manuel demo verisi (UI uzerinden)
1. `Jobs` ekraninda yeni job olustur ve publish et.
2. `Candidates` ekraninda yeni candidate olustur.
3. Candidate detail'de `sample-data/sample-candidate.pdf` veya `sample-data/sample-candidate.docx` yukle.
4. Parse tetikle ve profile olusumunu dogrula.
5. Job detail `Applications` panelinden candidate'i ilana bagla.
6. Interview session baslat.

## 3) Ornek payload dosyalari
- `sample-data/payloads/job-create.json`
- `sample-data/payloads/weights-competencies.json`
- `sample-data/payloads/weights-skills.json`
- `sample-data/payloads/application-create.json`
- `sample-data/payloads/human-override.json`

## 4) CV sample notu
- Dosya imzalari dogrulanmistir:
- PDF: `%PDF-`
- DOCX: `PK`
