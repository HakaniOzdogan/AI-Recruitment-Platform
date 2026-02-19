# UI Happy Path (Job -> Application -> Interview -> Scorecard)

Bu checklist tek oturumda en kritik zinciri dogrulamak icin kullanilir.

1) Login
- Adim: `/login` ile giris yap.
- Beklenen: `/jobs` sayfasi acilir, auth hatasi yok.

2) Job create + publish
- Adim: `Create Job` ile ilan olustur, sonra `Publish`.
- Beklenen: Job listesinde satir gorunur, status `Published` olur.

3) Candidate create
- Adim: `/candidates` sayfasinda `Create Candidate`.
- Beklenen: Candidate listesinde yeni satir gorunur.

4) Application olusturma
- Adim: Job detail -> `Applications` -> `Add candidate to job`.
- Beklenen: Applications tablosunda candidate satiri gorunur.

5) Stage update
- Adim: Application satirinda stage degistir + `Update Stage`.
- Beklenen: Satir yeni stage/status ile guncellenir.

6) Start interview
- Adim: `Start Interview` tikla.
- Beklenen: `/interviews/{sessionId}?jobId=...&candidateId=...&appId=...` route'una yonlenir.

7) Send message
- Adim: Interview chat input'una mesaj yaz, `Send`.
- Beklenen: Mesaj balonu listede gorunur; gerekirse system sorusu da eklenir.

8) Refresh messages
- Adim: `Refresh` tikla.
- Beklenen: Mesaj listesi yeniden yuklenir, kayip/bozulma olmaz.

9) Run auto score
- Adim: `Scorecard` panelinde `Run Auto Score`.
- Beklenen: Scorecard dolu gelir, en az 1 criterion satiri gorunur.

10) Human override
- Adim: Bir criterion icin `Override` ac, score+rationale+evidence gir, submit et.
- Beklenen:
- Bos/whitespace evidence engellenir.
- Basarili kayittan sonra scorecard refresh olur.
- ilgili criterion evaluator `HUMAN` gorunur.

## Edge-case mini checks
- Duplicate application (409): toast gorunur, panel kilitlenmez.
- Applications 403: sadece `Bu panel icin yetkin yok.` gorunur.
- Mode switch 409 (ADAPTIVE kapali): net hata/uyari mesaji gorunur.
- Scorecard empty (404): empty state + CTA gorunur.
