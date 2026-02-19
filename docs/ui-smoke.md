# UI Smoke Checklist

## 1) Jobs
- `Jobs` acilir.
- Bossa: "Henüz ilan yok" + `Create Job` butonu gorunur.
- Job olusturunca listede yeni satir gorunur.
- Publish sonrasi status `Published` olur.

## 2) Candidates
- `Candidates` acilir.
- Bossa: "Henüz aday yok" + `Create Candidate` butonu gorunur.
- Candidate olusturunca listede yeni satir gorunur.

## 3) Candidate Profile
- Candidate detail'de CV upload basarili.
- Parse sonrasi profile alanlari gelir.
- Beklenen: skills listesinde en az 1 etiket gorunur.

## 4) Applications
- Job detail `Applications` panelinde candidate eklenir.
- Bossa: "Bu ilana başvuru yok" + `Add candidate to job` CTA gorunur.

## 5) Interview
- `Start Interview` ile `/interviews/:sessionId` acilir.
- Mesaj gonderildiginde chat listesinde gorunur.
- Mesaj yoksa: "Henüz mesaj yok, ilk soruyu gönder" gorunur.

## 6) Scorecard
- `Run Auto Score` calisir.
- Beklenen: scorecard'da en az 1 criterion satiri gorunur.
