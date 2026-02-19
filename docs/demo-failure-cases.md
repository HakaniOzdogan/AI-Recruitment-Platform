# Demo Failure Cases (Kontrollu)

## 1) 403 Weights
Amac: Yetki eksiginde sayfa partial render davransin.

Adimlar:
1. `JOB_CREATE` izni olmayan bir kullanici ile login ol.
2. Bir job detail ac (`/jobs/{jobId}`).
3. Weights panelini ac.

Beklenen:
- Weights panelinde "yetkin yok" mesaji.
- Job header ve matching paneli calismaya devam eder.

## 2) 409 Adaptive Disabled
Amac: ADAPTIVE mod flag kapaliyken dogru hata gosterimi.

Adimlar:
1. `.env.prod` icinde `LLM_ENABLED=false` birak.
2. Interview sayfasinda mode'u `ADAPTIVE` sec.

Beklenen:
- API `409` doner.
- UI toast: "Adaptive enabled degil / LLM kapali".

## 3) 429 Rate Limit
Amac: Rate limiter gorunurlugu.

Adimlar:
1. Interview mesaj gonderme butonuna hizli sekilde ardisik 5-10 istek yap.
2. Gerekirse tarayici network tab ile tekrar et.

Beklenen:
- Bir noktada `429` donusu.
- UI hata mesajinda uygun bilgilendirme (ve varsa `Ref` correlation id).

## 4) Kanit toplama
- Failure-case adimlarindan sonra:
- Windows: `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1`
- Linux/Mac: `bash ops/evidence.sh`
