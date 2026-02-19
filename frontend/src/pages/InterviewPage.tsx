import { AxiosError } from "axios";
import { FormEvent, KeyboardEvent, useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import {
  getInterview,
  getInterviewAiDebug,
  getInterviewMessages,
  InterviewAiState,
  InterviewMessage,
  InterviewSession,
  postCandidateAnswer,
  postHiringMessage,
  postInterviewMessage,
  setInterviewMode
} from "../api/endpoints/interviews";
import { HumanOverridePayload, getScorecard, postHumanOverride, runAutoScore, ScorecardResponse } from "../api/endpoints/scoring";
import { ChatBubble } from "../components/ChatBubble";
import { Collapsible } from "../components/Collapsible";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { LoadingSkeleton } from "../components/LoadingSkeleton";
import { ModeToggle } from "../components/ModeToggle";
import { PageHeader } from "../components/PageHeader";
import { ScorePill } from "../components/ScorePill";
import { StatusBadge } from "../components/StatusBadge";
import { buildCandidateUrl } from "../routes/nav";
import { pushToast } from "../store/uiFeedback";
import { canApplyAndInterview, canManageApplications } from "../auth/capabilities";

type Mode = "OFF" | "ASSIST" | "ADAPTIVE";
type CriterionKey = HumanOverridePayload["criterionKey"];

const criterionKeys: CriterionKey[] = ["technical", "problem_solving", "communication", "culture_fit", "domain_knowledge"];

function parseDebugJson(value?: string | null): unknown {
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof AxiosError) {
    return (error.response?.data as { message?: string } | undefined)?.message ?? fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function toScorePillValue(score: number | null): number {
  if (score === null || Number.isNaN(score)) {
    return 0;
  }

  return Math.round((score / 5) * 100);
}

function extractPlanQuestions(debug: InterviewAiState | null): Array<{ text: string; category?: string; topic?: string }> {
  const parsed = parseDebugJson(debug?.latestPlanJson) as Record<string, unknown> | null;
  if (!parsed || typeof parsed !== "object") {
    return [];
  }

  const plan = parsed.planned_questions;
  if (!Array.isArray(plan)) {
    return [];
  }

  return plan
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }
      const row = item as Record<string, unknown>;
      const text = typeof row.question_text === "string" ? row.question_text : "";
      if (!text) {
        return null;
      }

      return {
        text,
        category: typeof row.category === "string" ? row.category : undefined,
        topic: typeof row.topic_key === "string" ? row.topic_key : undefined
      };
    })
    .filter((x): x is { text: string; category?: string; topic?: string } => x !== null);
}

function extractInsightSignals(debug: InterviewAiState | null): string[] {
  const parsed = parseDebugJson(debug?.latestInsightJson) as Record<string, unknown> | null;
  if (!parsed || typeof parsed !== "object") {
    return [];
  }

  const signals = parsed.signals;
  if (!Array.isArray(signals)) {
    return [];
  }

  return signals
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const row = item as Record<string, unknown>;
      const term = typeof row.term === "string" ? row.term : null;
      const topic = typeof row.topic_key === "string" ? row.topic_key : null;
      if (!term && !topic) {
        return null;
      }

      return term && topic ? `${term} (${topic})` : (term ?? topic ?? null);
    })
    .filter((x): x is string => !!x);
}

export function InterviewPage(): JSX.Element {
  const allowScoreControls = canManageApplications();
  const canSendAsCandidate = canApplyAndInterview() && !allowScoreControls;
  const { sessionId } = useParams();
  const [searchParams] = useSearchParams();
  const id = sessionId ?? "";
  const queryJobId = searchParams.get("jobId");
  const queryCandidateId = searchParams.get("candidateId");
  const queryAppId = searchParams.get("appId");

  const [mode, setMode] = useState<Mode>("OFF");

  const [interviewState, setInterviewState] = useState<{ loading: boolean; error: unknown; data: InterviewSession | null }>({
    loading: false,
    error: null,
    data: null
  });

  const [messagesState, setMessagesState] = useState<{
    loading: boolean;
    error: unknown;
    data: InterviewMessage[];
    unavailable: boolean;
    forbidden: boolean;
  }>({ loading: false, error: null, data: [], unavailable: false, forbidden: false });

  const [messageInput, setMessageInput] = useState("");
  const [sending, setSending] = useState(false);
  const [modeSwitching, setModeSwitching] = useState(false);

  const [scoreState, setScoreState] = useState<{
    loading: boolean;
    error: unknown;
    data: ScorecardResponse | null;
    forbidden: boolean;
    notice: string | null;
  }>({ loading: false, error: null, data: null, forbidden: false, notice: null });
  const [forceAuto, setForceAuto] = useState(true);
  const [autoScoring, setAutoScoring] = useState(false);

  const [debugState, setDebugState] = useState<{
    loading: boolean;
    error: unknown;
    data: InterviewAiState | null;
    unavailable: boolean;
  }>({ loading: false, error: null, data: null, unavailable: false });

  const [overrideOpen, setOverrideOpen] = useState(false);
  const [overrideCriterion, setOverrideCriterion] = useState<CriterionKey>("technical");
  const [overrideScore, setOverrideScore] = useState("3");
  const [overrideRationale, setOverrideRationale] = useState("");
  const [overrideEvidence, setOverrideEvidence] = useState("");
  const [overrideRelatedTo, setOverrideRelatedTo] = useState("");
  const [overrideSubmitting, setOverrideSubmitting] = useState(false);
  const [overrideClientError, setOverrideClientError] = useState<string | null>(null);

  async function loadInterview() {
    if (!id) {
      return;
    }

    setInterviewState((state) => ({ ...state, loading: true, error: null }));
    try {
      const session = await getInterview(id);
      setInterviewState({ loading: false, error: null, data: session });
      const modeValue = (session as InterviewSession & { aiMode?: string }).aiMode;
      if (modeValue === "OFF" || modeValue === "ASSIST" || modeValue === "ADAPTIVE") {
        setMode(modeValue);
      }
    } catch (error) {
      setInterviewState({ loading: false, error, data: null });
    }
  }

  async function loadMessages() {
    if (!id || messagesState.unavailable) {
      return;
    }

    setMessagesState((state) => ({ ...state, loading: true, error: null }));
    try {
      const data = await getInterviewMessages(id);
      setMessagesState({ loading: false, error: null, data, unavailable: false, forbidden: false });
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 404) {
        setMessagesState((state) => ({ ...state, loading: false, unavailable: true }));
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 403) {
        setMessagesState((state) => ({ ...state, loading: false, forbidden: true }));
        return;
      }
      setMessagesState((state) => ({ ...state, loading: false, error }));
    }
  }

  async function loadScorecard() {
    if (!id) {
      return;
    }

    setScoreState((state) => ({ ...state, loading: true, error: null, notice: null }));
    try {
      const data = await getScorecard(id);
      setScoreState({ loading: false, error: null, data, forbidden: false, notice: null });
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 404) {
        setScoreState((state) => ({ ...state, loading: false, data: null }));
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 403) {
        setScoreState((state) => ({ ...state, loading: false, forbidden: true }));
        return;
      }
      setScoreState((state) => ({ ...state, loading: false, error }));
    }
  }

  async function loadDebug() {
    if (!id) {
      return;
    }

    setDebugState((state) => ({ ...state, loading: true, error: null }));
    try {
      const data = await getInterviewAiDebug(id);
      setDebugState({ loading: false, error: null, data, unavailable: false });
    } catch (error) {
      if (error instanceof AxiosError && (error.response?.status === 403 || error.response?.status === 404)) {
        setDebugState({ loading: false, error: null, data: null, unavailable: true });
        return;
      }
      setDebugState({ loading: false, error, data: null, unavailable: false });
    }
  }

  useEffect(() => {
    void loadInterview();
    void loadMessages();
    if (allowScoreControls) {
      void loadScorecard();
      void loadDebug();
    }
  }, [id, allowScoreControls]);

  async function onModeChange(next: Mode) {
    if (!id) {
      return;
    }

    setModeSwitching(true);
    try {
      await setInterviewMode(id, { aiMode: next });
      await loadInterview();
      await loadDebug();
      setMode(next);
      pushToast("info", `Mode güncellendi: ${next}`);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 409) {
        pushToast("error", "Adaptive kapalı / LLM devre dışı");
      } else {
        pushToast("error", getErrorMessage(error, "Mode güncellenemedi"));
      }
    } finally {
      setModeSwitching(false);
    }
  }

  async function onSend(event: FormEvent) {
    event.preventDefault();
    if (!id || !messageInput.trim() || sending || messagesState.forbidden) {
      return;
    }

    const text = messageInput.trim();
    setSending(true);
    try {
      if (allowScoreControls) {
        await postHiringMessage(id, { text });
      } else if (canSendAsCandidate) {
        await postCandidateAnswer(id, { text });
      } else {
        const turn = await postInterviewMessage(id, { text });
        setMessagesState((state) => ({
          ...state,
          data: [
            ...state.data,
            { role: "candidate", content: text, createdAt: new Date().toISOString() },
            ...(turn.systemQuestion ? [{ role: "system" as const, content: turn.systemQuestion, createdAt: new Date().toISOString() }] : [])
          ]
        }));
      }

      setMessageInput("");
      if (!messagesState.unavailable) {
        await loadMessages();
      }
      if (allowScoreControls) {
        await loadDebug();
      }
    } catch (error) {
      setMessagesState((state) => ({ ...state, error }));
    } finally {
      setSending(false);
    }
  }

  function onMessageKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void onSend(event as unknown as FormEvent);
    }
  }

  async function onRunAutoScore() {
    if (!id) {
      return;
    }

    setAutoScoring(true);
    setScoreState((state) => ({ ...state, error: null, notice: null }));
    try {
      const result = await runAutoScore(id, forceAuto);
      setScoreState((state) => ({ ...state, data: result, forbidden: false, notice: "Auto score tamamlandı." }));
      await loadScorecard();
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setScoreState((state) => ({ ...state, forbidden: true, notice: "Scorecard için yetkin yok." }));
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 409) {
        setScoreState((state) => ({ ...state, notice: getErrorMessage(error, "Session scoring için hazır değil") }));
        return;
      }
      setScoreState((state) => ({ ...state, error }));
    } finally {
      setAutoScoring(false);
    }
  }

  function openOverrideModal(key: string) {
    const normalized = criterionKeys.includes(key as CriterionKey) ? (key as CriterionKey) : "technical";
    setOverrideCriterion(normalized);
    setOverrideScore("3");
    setOverrideRationale("");
    setOverrideEvidence("");
    setOverrideRelatedTo(normalized);
    setOverrideClientError(null);
    setOverrideOpen(true);
  }

  async function onSubmitOverride(event: FormEvent) {
    event.preventDefault();
    if (!id) {
      return;
    }

    const rationale = overrideRationale.trim();
    const evidence = overrideEvidence.trim();
    const score = Number(overrideScore);

    if (!rationale) {
      setOverrideClientError("Rationale zorunlu");
      return;
    }
    if (!evidence) {
      setOverrideClientError("Evidence quote zorunlu");
      return;
    }

    setOverrideSubmitting(true);
    setOverrideClientError(null);

    try {
      const payload: HumanOverridePayload = {
        criterionKey: overrideCriterion,
        score,
        rationale,
        evidenceQuotes: [
          {
            quote: evidence,
            relatedTo: (overrideRelatedTo || overrideCriterion).trim()
          }
        ]
      };

      const updated = await postHumanOverride(id, payload);
      setScoreState((state) => ({ ...state, data: updated, forbidden: false, notice: "Override kaydedildi" }));
      setOverrideOpen(false);
      await loadScorecard();
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setScoreState((state) => ({ ...state, forbidden: true, notice: "Override için yetkin yok" }));
      } else if (error instanceof AxiosError && error.response?.status === 400) {
        setOverrideClientError(getErrorMessage(error, "Validation hatası"));
      } else {
        setScoreState((state) => ({ ...state, error }));
      }
    } finally {
      setOverrideSubmitting(false);
    }
  }

  const plannedQuestions = useMemo(() => extractPlanQuestions(debugState.data), [debugState.data]);
  const insightSignals = useMemo(() => extractInsightSignals(debugState.data), [debugState.data]);
  const latestScores = useMemo(
    () => (scoreState.data?.latest ? [...scoreState.data.latest].sort((a, b) => a.criterionKey.localeCompare(b.criterionKey)) : []),
    [scoreState.data]
  );

  return (
    <section className="page-stack">
      <PageHeader
        title="Interview"
        subtitle={`Session: ${id}${interviewState.data?.status ? ` | ${interviewState.data.status}` : ""}`}
        actions={
          <>
            {queryJobId ? <Link to={`/jobs/${queryJobId}`}>Back Job</Link> : null}
            {queryCandidateId ? <Link to={buildCandidateUrl(queryCandidateId, queryJobId)}>Back Candidate</Link> : null}
            {queryJobId && queryAppId ? <Link to={`/jobs/${queryJobId}#applications`}>Back Application</Link> : null}
            {!queryJobId && !queryCandidateId && !queryAppId ? (
              <button type="button" className="button secondary" onClick={() => window.history.back()}>
                Back
              </button>
            ) : null}
          </>
        }
      />

      <div className="interview-layout">
        <div className="interview-main-column" style={{ gridColumn: "span 2" }}>
          <div className="card modern-card panel-card">
            <div className="row-actions" style={{ justifyContent: "space-between" }}>
              <h3 style={{ margin: 0 }}>Chat</h3>
              <div className="row-actions">
                {allowScoreControls ? <ModeToggle value={mode} onChange={(next) => void onModeChange(next)} disabled={modeSwitching} /> : null}
                <button type="button" className="button secondary" onClick={() => void loadMessages()} disabled={messagesState.loading || messagesState.forbidden}>
                  Refresh
                </button>
              </div>
            </div>

            {interviewState.loading ? <LoadingSkeleton lines={2} /> : null}
            {interviewState.error ? <ErrorState message={getErrorMessage(interviewState.error, "Interview yüklenemedi")} onRetry={() => void loadInterview()} /> : null}

            {messagesState.loading ? <LoadingSkeleton lines={4} /> : null}
            {messagesState.forbidden ? <ErrorState message="Messages için yetkin yok" /> : null}
            {messagesState.error ? <ErrorState message={getErrorMessage(messagesState.error, "Mesajlar yüklenemedi")} onRetry={() => void loadMessages()} /> : null}

            {!messagesState.loading && !messagesState.error && !messagesState.forbidden && messagesState.data.length === 0 ? (
              <EmptyState title="Henüz mesaj yok" message="İlk mesajı göndererek başlayabilirsin." />
            ) : null}

            <div className="chat-list">
              {messagesState.data.map((message, idx) => (
                <ChatBubble key={`${message.role}-${idx}`} role={message.role}>
                  {message.content}
                  <div className="muted">{message.createdAt ? new Date(message.createdAt).toLocaleString() : ""}</div>
                </ChatBubble>
              ))}
            </div>

            <form onSubmit={onSend} className="chat-input-sticky">
              <textarea
                rows={2}
                placeholder={allowScoreControls ? "Soru yaz..." : "Cevabini yaz..."}
                value={messageInput}
                onChange={(e) => setMessageInput(e.target.value)}
                onKeyDown={onMessageKeyDown}
                disabled={sending || messagesState.forbidden}
              />
              <button type="submit" className="button" disabled={sending || !messageInput.trim() || messagesState.forbidden}>
                {sending ? "Gönderiliyor..." : allowScoreControls ? "Soruyu Gönder" : "Cevap Gönder"}
              </button>
            </form>
          </div>
        </div>

        <div className="interview-side-column">
          {!allowScoreControls ? (
            <div className="card modern-card panel-card">
              <h3>Bilgi</h3>
              <p className="muted">Bu rolde sadece mülakat mesajlaşma akışı açıktır. Score/Debug yönetici panelindedir.</p>
            </div>
          ) : null}
          {allowScoreControls ? (
          <>
          <div className="card modern-card panel-card">
            <div className="row-actions" style={{ justifyContent: "space-between" }}>
              <h3 style={{ margin: 0 }}>Scorecard</h3>
              <div className="row-actions">
                {allowScoreControls ? (
                  <>
                    <label className="row-actions" style={{ marginBottom: 0 }}>
                      <input type="checkbox" checked={forceAuto} onChange={(e) => setForceAuto(e.target.checked)} />
                      force
                    </label>
                    <button type="button" className="button" onClick={() => void onRunAutoScore()} disabled={autoScoring || scoreState.forbidden}>
                      {autoScoring ? "Running..." : "Run Auto Score"}
                    </button>
                  </>
                ) : null}
              </div>
            </div>

            {scoreState.loading ? <LoadingSkeleton lines={5} /> : null}
            {scoreState.notice ? <p className="muted">{scoreState.notice}</p> : null}
            {scoreState.forbidden ? <ErrorState message="Scorecard için yetkin yok" /> : null}
            {scoreState.error ? <ErrorState message={getErrorMessage(scoreState.error, "Scorecard yüklenemedi")} onRetry={() => void loadScorecard()} /> : null}

            {!scoreState.loading && !scoreState.error && !scoreState.forbidden && !scoreState.data ? (
              <EmptyState title="Scorecard yok" message="Run Auto Score ile değerlendirmeyi başlat." actionLabel="Run Auto Score" onAction={() => void onRunAutoScore()} />
            ) : null}

            {scoreState.data ? (
              <>
                <div className="row-actions">
                  <strong>Overall</strong>
                  {scoreState.data.overallScore === null ? <span className="muted">null</span> : <ScorePill score={toScorePillValue(scoreState.data.overallScore)} />}
                  {scoreState.data.insufficientAll ? <StatusBadge label="Kanit yok" tone="insufficient" /> : null}
                </div>

                <div className="criteria-scroll">
                  {latestScores.map((criterion) => (
                    <div key={criterion.criterionKey} className="card" style={{ marginBottom: 8 }}>
                      <div className="row-actions" style={{ justifyContent: "space-between" }}>
                        <strong>{criterion.criterionKey}</strong>
                        <div className="row-actions">
                          {criterion.status === "INSUFFICIENT_EVIDENCE" ? (
                            <StatusBadge label="Kanit yok" tone="insufficient" />
                          ) : (
                            <StatusBadge label={String(criterion.score ?? "-")} tone="published" />
                          )}
                          <span className="muted">{criterion.evaluatorType}</span>
                          {allowScoreControls ? (
                            <button type="button" className="button secondary" onClick={() => openOverrideModal(criterion.criterionKey)} disabled={scoreState.forbidden}>
                              Override
                            </button>
                          ) : null}
                        </div>
                      </div>

                      <Collapsible title="Rationale & Evidence">
                        <p>{criterion.rationale ?? "-"}</p>
                        <ul>
                          {criterion.evidenceQuotes.map((e, idx) => (
                            <li key={`${criterion.criterionKey}-e-${idx}`}>
                              "{e.quote}" ({e.relatedTo})
                            </li>
                          ))}
                        </ul>
                      </Collapsible>
                    </div>
                  ))}
                </div>
              </>
            ) : null}
          </div>

          <Collapsible title="AI Debug">
            <div className="card modern-card panel-card" style={{ marginTop: 8 }}>
              <div className="row-actions" style={{ justifyContent: "space-between" }}>
                <div className="row-actions">
                  <StatusBadge label={`Guardrail: ${debugState.data?.guardrailBlocked ? "ON" : "OFF"}`} tone={debugState.data?.guardrailBlocked ? "insufficient" : "published"} />
                  <StatusBadge label={`Fallback: ${debugState.data?.fallbackUsed ? "USED" : "NO"}`} tone={debugState.data?.fallbackUsed ? "insufficient" : "ai"} />
                </div>
                <button type="button" className="button secondary" onClick={() => void loadDebug()} disabled={debugState.loading}>
                  Refresh
                </button>
              </div>

              {debugState.loading ? <LoadingSkeleton lines={3} /> : null}
              {debugState.unavailable ? <EmptyState title="Debug unavailable" message="Bu session için debug verisi yok." /> : null}
              {!debugState.unavailable && debugState.error ? (
                <ErrorState message={getErrorMessage(debugState.error, "Debug yüklenemedi")} onRetry={() => void loadDebug()} />
              ) : null}

              {debugState.data ? (
                <>
                  <strong>Insight</strong>
                  {insightSignals.length === 0 ? <p className="muted">Signal yok.</p> : <ul>{insightSignals.map((s, i) => <li key={`s-${i}`}>{s}</li>)}</ul>}
                  <strong>Planned Questions</strong>
                  {plannedQuestions.length === 0 ? (
                    <p className="muted">Planned question yok.</p>
                  ) : (
                    <ul>
                      {plannedQuestions.map((q, i) => (
                        <li key={`q-${i}`}>
                          {q.text}
                          {q.topic ? ` (${q.topic})` : ""}
                        </li>
                      ))}
                    </ul>
                  )}
                </>
              ) : null}
            </div>
          </Collapsible>
          </>
          ) : null}
        </div>
      </div>

      <div className="modal-overlay" style={{ display: overrideOpen ? "flex" : "none" }}>
        <div className="modal-card">
          <h3>Human Override</h3>
          <form onSubmit={onSubmitOverride} className="form-grid">
            <label>
              Criterion
              <select value={overrideCriterion} onChange={(e) => setOverrideCriterion(e.target.value as CriterionKey)}>
                {criterionKeys.map((key) => (
                  <option key={key} value={key}>
                    {key}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Score (0..5)
              <input type="number" min={0} max={5} step={1} value={overrideScore} onChange={(e) => setOverrideScore(e.target.value)} required />
            </label>

            <label>
              Rationale
              <textarea rows={3} value={overrideRationale} onChange={(e) => setOverrideRationale(e.target.value)} required />
            </label>

            <label>
              Evidence Quote (max 200)
              <textarea rows={2} value={overrideEvidence} onChange={(e) => setOverrideEvidence(e.target.value)} required maxLength={200} />
            </label>

            <label>
              Related To
              <input value={overrideRelatedTo} onChange={(e) => setOverrideRelatedTo(e.target.value)} placeholder={overrideCriterion} />
            </label>

            {overrideClientError ? <p className="inline-error">{overrideClientError}</p> : null}

            <div className="row-actions">
              <button type="button" className="button secondary" onClick={() => setOverrideOpen(false)} disabled={overrideSubmitting}>
                Cancel
              </button>
              <button type="submit" className="button" disabled={overrideSubmitting || scoreState.forbidden}>
                {overrideSubmitting ? "Kaydediliyor..." : "Submit Override"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </section>
  );
}

