# Demo Smoke Checklist

## 1) Job -> Weights -> Matching
1. Login ol (`/login`).
2. `Jobs` sayfasında yeni job oluştur.
3. Job detail'e gir, `Publish` çalıştır.
4. Weights panelinde competency toplamını 1.00 yap ve kaydet.
5. Skill weights ekle/kaydet.
6. Matching panelinde `minScore` verip `Load Matches` çalıştır.
7. Beklenen: liste gelir veya "Eşleşme yok. Min score değerini düşür" mesajı görünür.

## 2) Candidate -> CV Upload/Parse -> Profile
1. `Candidates` sayfasında aday oluştur.
2. Candidate detail'de CV yükle (pdf/docx).
3. `Parse CV` çalıştır.
4. `Candidate Profile` panelinde summary/skills/experience görün.
5. Beklenen: parse sonrası profile dolu, parse öncesi empty state görünür.

## 3) AI Evaluation
1. Candidate detail'de job seç.
2. `Load Report` dene.
3. 404 ise `Force Evaluate` çalıştır.
4. 409 gelirse "Aday rızası gerekli" mesajı gör.
5. Beklenen: report varsa recommendation/strengths/risks render edilir.

## 4) Application -> Interview -> Chat
1. Job detail `Applications` panelinde adayı ilana bağla.
2. Stage güncelle.
3. `Start Interview` veya mevcut sessionId ile `Open Interview` aç.
4. Interview sayfasında mesaj gönder.
5. `Mode` OFF/ASSIST/ADAPTIVE değiştir.
6. ADAPTIVE 409 ise "Adaptive enabled değil / LLM kapalı" mesajı görün.

## 5) Scorecard -> Human Override
1. Interview sayfasında `Run Auto Score` çalıştır.
2. Scorecard'da kriterleri kontrol et.
3. `INSUFFICIENT_EVIDENCE` olan kriterde "Kanıt yok" label'ı doğrula.
4. Bir kriterde `Override` aç, rationale + evidence gir, submit et.
5. Beklenen: kriter evaluator = HUMAN olur, scorecard yenilenir.

## Hızlı Regresyon Kontrolleri
- 401: login'e geri yönlendirme.
- 403: ilgili panelde "Yetkin yok" + aksiyon disable.
- 400: JsonErrorBox alan bazlı validasyon.
- 409: anlaşılır conflict mesajı.
- Hata toastlarında `Ref: <correlationId>` görünmeli.
