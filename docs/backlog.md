# Backlog

Teslim sonrasi gelistirme plani (tek sayfa).

## P0 (Zorunlu)
1. **Full endpoint-level integration tests (WebApplicationFactory) genislet**
- Aciklama: Auth, ATS, CV, Interview, Scoring, AI evaluation ve edge-case zincirlerini daha genis endpoint seviyesinde test etmek.
- Efor: M
- Etki/Risk: Yuksek etki; prod regresyon ve beklenmeyen kirilma riskini dusurur.

2. **Dashboard + basic analytics**
- Aciklama: Job funnel, stage conversion, time-to-hire, parse/match/scoring temel KPI gorunurlugu.
- Efor: M
- Etki/Risk: Yuksek etki; operasyonel karar kalitesini artirir, gorunurluk eksigi riskini azaltir.

3. **Data retention/anonymization policy + export deletion**
- Aciklama: Retention suresi dolan aday verisinin anonimlestirilmesi/silinmesi ve disa aktarilan veride silme taleplerinin uygulanmasi.
- Efor: L
- Etki/Risk: Yuksek etki; KVKK/uyumluluk riskini azaltir.

## P1 (Urunlestirme)
4. **Role/permission yonetim UI**
- Aciklama: Admin tarafinda rol olusturma, permission atama ve kullanici-rol yonetimi ekranlari.
- Efor: M
- Etki/Risk: Orta-yuksek etki; manuel operasyon yukunu azaltir.

5. **Candidate deduplication**
- Aciklama: Email/telefon/benzer profil sinyalleriyle tekrar aday kayitlarini birlestirme veya isaretleme.
- Efor: M
- Etki/Risk: Orta etki; veri kalitesi ve match dogrulugu artar.

6. **Export (CSV/PDF) job/candidate/interview raporlari**
- Aciklama: Temel raporlarin yonetim ve denetim icin disa aktarimi.
- Efor: M
- Etki/Risk: Orta etki; raporlama surecini hizlandirir.

7. **Interview transcript search & filter**
- Aciklama: Oturum mesajlari icinde hizli arama ve filtreleme (criterion/topic bazli).
- Efor: S/M
- Etki/Risk: Orta etki; HR review suresini kisaltir.

## P2 (Nice-to-have)
8. **Notification hooks (email/slack)**
- Aciklama: Kritik olaylar (new application, stage change, score ready) icin entegrasyon hooklari.
- Efor: M
- Etki/Risk: Dusuk-orta etki; takip kolayligi saglar.

9. **Dark mode + theme**
- Aciklama: Demo UI icin tema secimi ve temel gorunum iyilestirmesi.
- Efor: S
- Etki/Risk: Dusuk etki; urun algisini iyilestirir.

10. **Advanced matching explanation (LLM rationale)**
- Aciklama: Deterministik skora ek olarak kontrollu LLM destekli aciklama katmani.
- Efor: M
- Etki/Risk: Orta etki; aciklanabilirlik artar, ancak hallucination riski guardrail gerektirir.
