# API

## Rubric (Job Bazlı)
- `GET /jobs/{jobId}/rubric`
  - Job rubric varsa onu döner, yoksa default rubric.
- `PUT /jobs/{jobId}/rubric`
  - Body:
  ```json
  {
    "name": "Backend Interview Rubric",
    "criteria": [
      { "key": "technical", "title": "Technical", "description": "...", "weight": 0.30, "order": 1 },
      { "key": "problem_solving", "title": "Problem Solving", "description": "...", "weight": 0.25, "order": 2 },
      { "key": "communication", "title": "Communication", "description": "...", "weight": 0.20, "order": 3 },
      { "key": "culture_fit", "title": "Culture Fit", "description": "...", "weight": 0.10, "order": 4 },
      { "key": "domain_knowledge", "title": "Domain Knowledge", "description": "...", "weight": 0.15, "order": 5 }
    ]
  }
  ```

Kurallar:
- Criteria sayısı `1..10`.
- Key allowlist: `technical`, `problem_solving`, `communication`, `culture_fit`, `domain_knowledge`.
- Weight `0..1`.
- Toplam weight `1.0 ± 0.01`.

Yetki:
- `JOB_CREATE`

## Interview Scoring
- `POST /interviews/{sessionId}/score/auto?force=true|false`
  - Session `Completed` değilse `409`.
  - `force=false`: mevcut AI score varsa idempotent olarak onu döner.
  - `force=true`: yeni AI score seti üretir, history korunur.
- `POST /interviews/{sessionId}/score/human`
  - Body:
  ```json
  {
    "criterionKey": "technical",
    "score": 4,
    "rationale": "Candidate gave concrete architecture and scaling examples.",
    "evidenceQuotes": [
      { "quote": "I designed a multi-tenant .NET API and reduced p95 from 800ms to 250ms.", "relatedTo": "technical" }
    ]
  }
  ```
- `GET /interviews/{sessionId}/score`
  - Scorecard + latest criterion skorları + history döner.

Yetki:
- `CANDIDATE_MANAGE` veya `APPLICATION_STAGE_UPDATE`

## Adaptive Interview
- `PATCH /interviews/{sessionId}/mode`
  - Body:
  ```json
  { "aiMode": "OFF|ASSIST|ADAPTIVE" }
  ```
  - Yetki: `CANDIDATE_MANAGE`

- `POST /interviews/{sessionId}/messages`
  - Candidate mesajını kaydeder.
  - `AiMode=ADAPTIVE` ise analyze + plan üretir ve system sorusunu dinamik seçer/üretir.
  - LLM hata veya invalid schema durumunda template fallback kullanır.
  - Yetki: `CANDIDATE_MANAGE` veya `APPLICATION_STAGE_UPDATE`

- `GET /interviews/{sessionId}/ai`
  - Son insight + plan + guardrail/fallback bilgisini döner.
  - Yetki: `CANDIDATE_MANAGE` veya `APPLICATION_STAGE_UPDATE`

### Analyze JSON (strict)
```json
{
  "signals": [{"term":"CQRS","topic_key":"cqrs","confidence":0.82}],
  "competency_estimates": {"technical":3.5,"problem_solving":3.0,"communication":2.8,"culture_fit":2.6,"domain_knowledge":3.2},
  "depth": {"clarity":3.0,"specificity":2.7},
  "risk_flags": {"vague_answer":false,"contradiction":false,"overclaim":false},
  "evidence_snippets": [{"quote":"...","related_to":"technical"}],
  "next_focus": [{"topic_key":"cqrs","why":"Implementation detail is shallow","priority":1}]
}
```

### Plan JSON (strict)
```json
{
  "plan_horizon": 3,
  "planned_questions": [
    {
      "type":"bank|generated",
      "category":"technical|behavioral|case|culture",
      "topic_key":"cqrs",
      "difficulty":3,
      "question_text":"...",
      "why":"...",
      "guardrails_ok":true
    }
  ]
}
```

Guardrails:
- LLM input yalnızca son `N` mesaj (varsayılan 8) + job/rubric özeti içerir.
- Hassas alanlar bloklanır.
- `question_text` max 350 karakter.
- Evidence snippet max 200 karakter.

## Deterministic Evidence + Score Kuralı
- Sadece candidate mesajları kullanılır (yoksa tüm transcript fallback).
- Criterion başına `1..3` alıntı seçilir.
- Her quote sanitize edilir:
  - satır sonları temizlenir
  - max `200` karakter
- Skorlama (placeholder):
  - `0`: kanıt yok (`insufficient_evidence`)
  - `1`: çok genel/zayıf
  - `3`: orta düzey + örnek
  - `5`: detay + spesifik + ölçülebilir sonuç

## OverallScore Hesabı
- Son (latest) skorlar baz alınır.
- Kanıtsız/`insufficient_evidence` criterion hesap dışı kalır.
- Kalan criterionlar için ağırlıklı ortalama hesaplanır.
- Kullanılabilir criterion yoksa `OverallScore = null`.

## LLM Assisted Mode
- `SCORING_MODE=deterministic|llm_assisted`
- `llm_assisted` modunda deterministic çıktı best-effort LLM ile zenginleştirilir.
- LLM başarısız olursa deterministic skor fallback olarak kalır.

## Audit Actions
- `AUTO_SCORE_RUN`
- `HUMAN_SCORE_OVERRIDE`

## Hata Kodları
- `400` validation
- `401` unauthorized
- `403` forbidden
- `404` not found
- `409` business conflict (örn: session completed değil)

## Bugfix Doğrulama (Swagger)
1. `POST /interviews/{sessionId}/score/auto`:
   - Evidence üretilemeyen criterion için `status=INSUFFICIENT_EVIDENCE`, `score=null` beklenir.
   - `score=0` yazılmamalı.
2. `GET /jobs/{jobId}/weights`:
   - Yetkisiz kullanıcıda `401/403`, `JOB_CREATE` yetkisinde `200`.
3. `POST /interviews/{sessionId}/score/human`:
   - Sadece whitespace evidence gönderildiğinde `400` ve `errors.evidenceQuotes` alanı beklenir.
