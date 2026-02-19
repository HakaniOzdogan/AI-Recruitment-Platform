# Demo 10 Min Cheatsheet

## 0) On Hazirlik (1 dk)
- `run-demo` calismis olmali.
- Kontrol:
- Web: `http://localhost:3000`
- API: `http://localhost:8080/swagger`
- Acik sekmeler:
- `/jobs`
- `/candidates`
- bir job detail (`/jobs/{jobId}`)
- bir interview (`/interviews/{sessionId}`)

## 1) Ana Akis (8 dk)
1. Jobs (Create + Publish)
- `Jobs` sayfasinda `Create Job`.
- Job satiri olustugunu ve `Publish` sonrasi status `Published` oldugunu goster.

2. Weights
- Job detail'e gir.
- Competency weights'te 2 alan degistir ve toplam `1.0` oldugunu goster.

3. Candidates + CV + Parse
- `Candidates` sayfasinda `Create Candidate`.
- Candidate detail'de CV upload (`sample-data`) + `Parse CV`.
- Profile'da skills/experience gorundugunu goster.

4. Matching
- Job detail `Matching` panelinde shortlist goster.
- Bir satirdan `Open Candidate` ile navigation goster.

5. Applications
- Job detail `Applications` panelinde `Add candidate to job`.
- Application satirinin olustugunu goster.

6. Interview Baslat
- Ayni satirdan `Start Interview`.
- Query paramli route'a gittigini goster (`jobId/candidateId/appId`).

7. Interview Chat
- 1 mesaj gonder (`Send`).
- Mesaj balonunu ve `Refresh` davranisini goster.

8. Scorecard
- `Run Auto Score`.
- En az 1 criterion satiri + status goruntule.

9. Human Override
- Bir criterion icin `Override` ac.
- Evidence + rationale ile submit et.
- evaluator `HUMAN` olarak guncellendigini goster.

10. AI Evaluation
- Candidate detail'e don.
- Job secip `Force Evaluate`.
- Recommendation + confidence goster.

## 2) Failure-Case (30 sn)
Secenek A (onerilen): `409 Adaptive disabled`
- Interview mode'u `ADAPTIVE` yapmayi dene (LLM kapaliysa).
- UI'da 409/toast mesajini goster.

Secenek B: `403 Weights`
- Dusuk yetkili user ile job detail ac.
- Weights panelinde `Yetkin yok` mesajini goster.

## 3) Kapanis (30 sn)
- Evidence klasoru goster: `evidence/YYYYMMDD-HHMM/`.
- Submission paketi goster:
- `ik-otomasyon-submission-v1.0.0.zip`
- `submission/MANIFEST.txt`

## Not
- Takilirsa hizli fallback:
- `docs/demo-seed-5min.md`
- `docs/ui-happy-path.md`
- `docs/evidence-pack.md`
