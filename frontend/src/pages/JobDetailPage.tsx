import { AxiosError } from "axios";
import { Lock } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  Application,
  createApplication,
  getApplicationsByJob,
  startInterview,
  updateApplicationStage
} from "../api/endpoints/applications";
import { Candidate, getCandidates } from "../api/endpoints/candidates";
import {
  getJob,
  getJobWeights,
  getMatches,
  Job,
  JobSkillWeight,
  JobWeightsResponse,
  MatchListItem,
  publishJob,
  toApiMinScore,
  updateCompetencyWeights,
  updateSkillWeights
} from "../api/endpoints/jobs";
import { CandidateSelect } from "../components/CandidateSelect";
import { Collapsible } from "../components/Collapsible";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { JsonErrorBox } from "../components/JsonErrorBox";
import { LoadingSkeleton } from "../components/LoadingSkeleton";
import { PanelCard } from "../components/PanelCard";
import { ScorePill } from "../components/ScorePill";
import { StageSelect } from "../components/StageSelect";
import { StatusBadge } from "../components/StatusBadge";
import { buildCandidateUrl, buildInterviewUrl } from "../routes/nav";
import { pushToast } from "../store/uiFeedback";

const competencyKeys = ["technical", "problem_solving", "communication", "culture_fit", "domain_knowledge"];

type SkillRow = {
  name: string;
  weight: string;
  isRequired: boolean;
};

type ResourceState<T> = {
  data: T;
  loading: boolean;
  error: unknown;
};

type WeightsState = ResourceState<JobWeightsResponse | null> & {
  forbidden: boolean;
};

function toSkillRows(skills: JobSkillWeight[]): SkillRow[] {
  return skills.map((item) => ({
    name: item.name,
    weight: String(item.weight),
    isRequired: item.isRequired
  }));
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

function ForbiddenState({ message }: { message: string }): JSX.Element {
  return (
    <div className="forbidden-box">
      <div className="row-actions">
        <Lock size={16} />
        <strong>Yetkin yok</strong>
      </div>
      <p>{message}</p>
    </div>
  );
}

export function JobDetailPage(): JSX.Element {
  const { id } = useParams();
  const navigate = useNavigate();
  const jobId = id ?? "";

  const [jobState, setJobState] = useState<ResourceState<Job | null>>({ data: null, loading: false, error: null });
  const [weightsState, setWeightsState] = useState<WeightsState>({ data: null, loading: false, error: null, forbidden: false });
  const [matchesState, setMatchesState] = useState<ResourceState<MatchListItem[]>>({ data: [], loading: false, error: null });
  const [applicationsState, setApplicationsState] = useState<ResourceState<Application[]>>({ data: [], loading: false, error: null });
  const [candidatesState, setCandidatesState] = useState<ResourceState<Candidate[]>>({ data: [], loading: false, error: null });
  const [matchesForbidden, setMatchesForbidden] = useState(false);
  const [applicationsForbidden, setApplicationsForbidden] = useState(false);
  const [candidatesForbidden, setCandidatesForbidden] = useState(false);

  const [competencies, setCompetencies] = useState<Record<string, number>>({});
  const [skills, setSkills] = useState<SkillRow[]>([]);
  const [minScore, setMinScore] = useState(50);
  const [savingComp, setSavingComp] = useState(false);
  const [savingSkills, setSavingSkills] = useState(false);
  const [publishOpen, setPublishOpen] = useState(false);

  const [addModalOpen, setAddModalOpen] = useState(false);
  const [candidateIdToAdd, setCandidateIdToAdd] = useState("");
  const [stageInputByApp, setStageInputByApp] = useState<Record<string, string>>({});
  const [startedSessionByApp, setStartedSessionByApp] = useState<Record<string, string>>({});
  const [appActionError, setAppActionError] = useState<string | null>(null);
  const matchRequestAbortRef = useRef<AbortController | null>(null);
  const matchRequestKeyRef = useRef<string>("");
  const [startingInterviewAppId, setStartingInterviewAppId] = useState<string | null>(null);

  const totalWeight = useMemo(
    () => competencyKeys.reduce((sum, key) => sum + (Number.isFinite(competencies[key]) ? competencies[key] : 0), 0),
    [competencies]
  );
  const competencyValid = Math.abs(totalWeight - 1) <= 0.01;

  const stageOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const app of applicationsState.data) {
      map.set(app.stageId, app.stageName);
    }

    return Array.from(map.entries()).map(([idValue, name]) => ({ id: idValue, name }));
  }, [applicationsState.data]);

  async function loadJob() {
    if (!jobId) {
      return;
    }

    setJobState((state) => ({ ...state, loading: true, error: null }));
    try {
      const jobData = await getJob(jobId);
      setJobState({ data: jobData, loading: false, error: null });
    } catch (error) {
      setJobState({ data: null, loading: false, error });
    }
  }

  async function loadWeights() {
    if (!jobId) {
      return;
    }

    setWeightsState((state) => ({ ...state, loading: true, error: null, forbidden: false }));
    try {
      const weightData = await getJobWeights(jobId);
      setWeightsState({ data: weightData, loading: false, error: null, forbidden: false });

      const seeded: Record<string, number> = {};
      for (const key of competencyKeys) {
        seeded[key] = weightData.competencies[key] ?? 0;
      }

      setCompetencies(seeded);
      setSkills(toSkillRows(weightData.skills));
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setWeightsState({ data: null, loading: false, error: null, forbidden: true });
        return;
      }

      setWeightsState({ data: null, loading: false, error, forbidden: false });
    }
  }

  async function loadMatches() {
    if (!jobId || !jobState.data) {
      return;
    }

    setMatchesState((state) => ({ ...state, loading: true, error: null }));
    setMatchesForbidden(false);
    matchRequestAbortRef.current?.abort();
    const controller = new AbortController();
    matchRequestAbortRef.current = controller;
    const requestMinScore = toApiMinScore(minScore);
    const requestKey = `${jobId}:${requestMinScore}`;
    matchRequestKeyRef.current = requestKey;
    try {
      const data = await getMatches(jobId, { minScore: requestMinScore }, controller.signal);
      if (matchRequestKeyRef.current !== requestKey) {
        return;
      }
      setMatchesState({ data, loading: false, error: null });
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 403) {
        setMatchesForbidden(true);
        setMatchesState((state) => ({ ...state, loading: false, error: null }));
        return;
      }
      setMatchesState((state) => ({ ...state, loading: false, error }));
    }
  }

  async function loadApplications() {
    if (!jobId) {
      return;
    }

    setApplicationsState((state) => ({ ...state, loading: true, error: null }));
    setApplicationsForbidden(false);
    try {
      const data = await getApplicationsByJob(jobId);
      setStartedSessionByApp((existing) => {
        const next = { ...existing };
        for (const app of data) {
          const sessionId = app.sessionId ?? app.interviewSessionId ?? app.interviewId ?? null;
          if (sessionId) {
            next[app.id] = sessionId;
          }
        }
        return next;
      });
      setApplicationsState({ data, loading: false, error: null });
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setApplicationsForbidden(true);
        setApplicationsState((state) => ({ ...state, loading: false, error: null }));
        return;
      }
      setApplicationsState((state) => ({ ...state, loading: false, error }));
    }
  }

  async function loadCandidates() {
    setCandidatesState((state) => ({ ...state, loading: true, error: null }));
    setCandidatesForbidden(false);
    try {
      const data = await getCandidates();
      setCandidatesState({ data, loading: false, error: null });
      if (!candidateIdToAdd && data[0]) {
        setCandidateIdToAdd(data[0].id);
      }
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setCandidatesForbidden(true);
        setCandidatesState((state) => ({ ...state, loading: false, error: null }));
        return;
      }
      setCandidatesState((state) => ({ ...state, loading: false, error }));
    }
  }

  useEffect(() => {
    void loadJob();
    void loadWeights();
    void loadApplications();
    setCandidatesState({ data: [], loading: false, error: null });
    setCandidateIdToAdd("");
    setMatchesState({ data: [], loading: false, error: null });
    setAppActionError(null);
    setStartedSessionByApp({});
    setMatchesForbidden(false);
    setApplicationsForbidden(false);
    setCandidatesForbidden(false);
    matchRequestAbortRef.current?.abort();

    return () => {
      matchRequestAbortRef.current?.abort();
    };
  }, [jobId]);

  useEffect(() => {
    if (!addModalOpen) {
      return;
    }

    void loadCandidates();
  }, [addModalOpen]);

  useEffect(() => {
    if (!jobState.data) {
      return;
    }

    const handle = setTimeout(() => {
      void loadMatches();
    }, 300);

    return () => clearTimeout(handle);
  }, [minScore, jobState.data?.id]);

  async function saveCompetencies() {
    if (!jobId || !competencyValid || weightsState.forbidden) {
      return;
    }

    setSavingComp(true);
    setWeightsState((state) => ({ ...state, error: null }));
    try {
      const updated = await updateCompetencyWeights(jobId, competencies);
      setWeightsState({ data: updated, loading: false, error: null, forbidden: false });
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setWeightsState({ data: null, loading: false, error: null, forbidden: true });
      } else {
        setWeightsState((state) => ({ ...state, error }));
      }
    } finally {
      setSavingComp(false);
    }
  }

  async function saveSkills() {
    if (!jobId || weightsState.forbidden) {
      return;
    }

    setSavingSkills(true);
    setWeightsState((state) => ({ ...state, error: null }));
    try {
      const payload: JobSkillWeight[] = skills
        .map((row) => ({
          name: row.name.trim().toLowerCase(),
          weight: Number(row.weight),
          isRequired: row.isRequired
        }))
        .filter((row) => row.name.length > 0);

      const updated = await updateSkillWeights(jobId, payload);
      setWeightsState({ data: updated, loading: false, error: null, forbidden: false });
      setSkills(toSkillRows(updated.skills));
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setWeightsState({ data: null, loading: false, error: null, forbidden: true });
      } else {
        setWeightsState((state) => ({ ...state, error }));
      }
    } finally {
      setSavingSkills(false);
    }
  }

  async function onPublish() {
    if (!jobId || !jobState.data) {
      return;
    }

    setJobState((state) => ({ ...state, error: null }));
    try {
      const updated = await publishJob(jobId);
      setJobState((state) => ({ ...state, data: updated }));
    } catch (error) {
      setJobState((state) => ({ ...state, error }));
    } finally {
      setPublishOpen(false);
    }
  }

  async function onAddApplication() {
    if (!jobId || !candidateIdToAdd) {
      return;
    }

    setAppActionError(null);
    try {
      await createApplication({ jobId, candidateId: candidateIdToAdd });
      await loadApplications();
      setAddModalOpen(false);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setApplicationsForbidden(true);
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 409) {
        setAppActionError("Bu aday için zaten başvuru var");
        pushToast("error", "Bu aday için zaten başvuru var");
        await loadApplications();
      } else {
        setAppActionError(getErrorMessage(error, "Başvuru oluşturulamadı"));
      }
    }
  }

  async function onUpdateStage(appId: string) {
    const stageId = stageInputByApp[appId]?.trim();
    if (!stageId) {
      setAppActionError("Geçerli bir stage seçin veya stageId girin");
      return;
    }

    setAppActionError(null);
    try {
      await updateApplicationStage(appId, { stage: stageId });
      await loadApplications();
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setApplicationsForbidden(true);
        return;
      }
      setAppActionError(getErrorMessage(error, "Stage güncellenemedi"));
    }
  }

  async function onStartInterview(appId: string, candidateId: string) {
    setAppActionError(null);
    setStartingInterviewAppId(appId);
    try {
      const result = await startInterview(appId);
      if (result.sessionId) {
        setStartedSessionByApp((state) => ({ ...state, [appId]: result.sessionId }));
        navigate(buildInterviewUrl(result.sessionId, jobId, candidateId, appId));
      }
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setApplicationsForbidden(true);
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 404) {
        setAppActionError("Interview start endpoint bulunamadı. SessionId varsa elle açabilirsiniz.");
      } else {
        setAppActionError(getErrorMessage(error, "Interview başlatılamadı"));
      }
    } finally {
      setStartingInterviewAppId(null);
    }
  }

  const jobError404 = jobState.error instanceof AxiosError && jobState.error.response?.status === 404;

  return (
    <section className="page-stack">
      <div className="card modern-card">
        {jobState.loading ? <LoadingSkeleton lines={2} /> : null}
        {jobError404 ? <ErrorState message="Job bulunamadı" onRetry={() => void loadJob()} /> : null}
        {!jobError404 && jobState.error ? <ErrorState message={getErrorMessage(jobState.error, "Job yüklenemedi")} onRetry={() => void loadJob()} /> : null}

        {jobState.data ? (
          <div className="row-actions" style={{ justifyContent: "space-between", width: "100%" }}>
            <div>
              <h2 style={{ margin: 0 }}>{jobState.data.title}</h2>
              <p className="muted">
                JobId: {jobState.data.id} | Updated: {new Date(jobState.data.publishedAt ?? jobState.data.createdAt).toLocaleString()}
              </p>
            </div>
            <div className="row-actions">
              <StatusBadge label={jobState.data.status} />
              {jobState.data.status !== "Published" && jobState.data.status !== "Closed" ? (
                <button className="button" type="button" onClick={() => setPublishOpen(true)}>
                  Publish
                </button>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>

      <div className="job-detail-grid">
        <PanelCard title="Weights" actions={<button type="button" onClick={() => void loadWeights()} disabled={weightsState.loading}>Retry</button>}>
          {weightsState.loading ? <LoadingSkeleton lines={6} /> : null}

          {weightsState.forbidden ? <ForbiddenState message="Weights panelini görüntülemek için yetkin yok." /> : null}

          {!weightsState.forbidden && weightsState.error ? (
            <ErrorState message={getErrorMessage(weightsState.error, "Weights yüklenemedi")} onRetry={() => void loadWeights()} />
          ) : null}

          {!weightsState.forbidden && !weightsState.loading && !weightsState.error && !weightsState.data ? (
            <EmptyState title="Weights yok" message="Bu job için henüz ağırlık tanımlanmamış." actionLabel="Retry" onAction={() => void loadWeights()} />
          ) : null}

          {!weightsState.forbidden && !weightsState.loading && !weightsState.error && weightsState.data ? (
            <>
              <h3>Competency Weights</h3>
              <div className="form-grid">
                {competencyKeys.map((key) => (
                  <label key={key}>
                    {key}
                    <input
                      type="number"
                      min={0}
                      max={1}
                      step="0.01"
                      value={competencies[key] ?? 0}
                      onChange={(e) =>
                        setCompetencies((state) => ({
                          ...state,
                          [key]: Number(e.target.value)
                        }))
                      }
                    />
                  </label>
                ))}
              </div>
              <p>Total: {totalWeight.toFixed(2)} {competencyValid ? "" : "(1.00 olmalı)"}</p>
              <button type="button" disabled={!competencyValid || savingComp} onClick={() => void saveCompetencies()}>
                {savingComp ? "Kaydediliyor..." : "Save Competencies"}
              </button>

              <h3>Skill Weights</h3>
              <div className="row-actions">
                <button type="button" onClick={() => setSkills((rows) => [...rows, { name: "", weight: "0", isRequired: false }])}>
                  + Add Skill
                </button>
              </div>

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Skill</th>
                      <th>Weight</th>
                      <th>Required</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {skills.map((row, index) => (
                      <tr key={`${row.name}-${index}`}>
                        <td>
                          <input
                            value={row.name}
                            onChange={(e) =>
                              setSkills((list) => list.map((item, i) => (i === index ? { ...item, name: e.target.value } : item)))
                            }
                          />
                        </td>
                        <td>
                          <input
                            type="number"
                            min={0}
                            max={1}
                            step="0.01"
                            value={row.weight}
                            onChange={(e) =>
                              setSkills((list) => list.map((item, i) => (i === index ? { ...item, weight: e.target.value } : item)))
                            }
                          />
                        </td>
                        <td>
                          <input
                            type="checkbox"
                            checked={row.isRequired}
                            onChange={(e) =>
                              setSkills((list) => list.map((item, i) => (i === index ? { ...item, isRequired: e.target.checked } : item)))
                            }
                          />
                        </td>
                        <td>
                          <button type="button" onClick={() => setSkills((list) => list.filter((_, i) => i !== index))}>
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <button type="button" onClick={() => void saveSkills()} disabled={savingSkills}>
                {savingSkills ? "Kaydediliyor..." : "Save Skills"}
              </button>
            </>
          ) : null}
        </PanelCard>

        <PanelCard
          title="Matching"
          actions={
            <div className="row-actions">
              <label>
                Min Score
                <input type="number" min={0} max={100} value={minScore} onChange={(e) => setMinScore(Number(e.target.value))} />
              </label>
              <button type="button" onClick={() => setMinScore(50)} disabled={matchesState.loading}>50</button>
              <button type="button" onClick={() => setMinScore(30)} disabled={matchesState.loading}>30</button>
              <button type="button" onClick={() => setMinScore(10)} disabled={matchesState.loading}>10</button>
              <button type="button" onClick={() => void loadMatches()} disabled={matchesState.loading || !jobState.data || matchesForbidden}>Retry</button>
            </div>
          }
        >
          {jobState.loading || matchesState.loading ? <LoadingSkeleton lines={6} /> : null}
          {matchesForbidden ? <ForbiddenState message="Matching panelini görüntülemek için yetkin yok." /> : null}
          {!matchesForbidden && matchesState.error ? <ErrorState message={getErrorMessage(matchesState.error, "Matches yüklenemedi")} onRetry={() => void loadMatches()} /> : null}

          {!matchesState.loading && !matchesState.error && !matchesForbidden && matchesState.data.length === 0 ? (
            <EmptyState
              title="Eşleşme yok"
              message="minScore değerini düşürüp tekrar deneyebilirsin."
              actionLabel="minScore=30"
              onAction={() => setMinScore(30)}
            />
          ) : null}

          {!matchesState.loading && !matchesForbidden && matchesState.data.length > 0 ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Candidate</th>
                    <th>Score</th>
                    <th>Stage</th>
                    <th>Details</th>
                  </tr>
                </thead>
                <tbody>
                  {matchesState.data.map((item) => (
                    <tr key={item.applicationId}>
                      <td>
                        {item.candidateFullName}
                        <br />
                        <small>{item.candidateId}</small>
                        <br />
                        <Link to={buildCandidateUrl(item.candidateId, jobId)}>Open Candidate</Link>
                      </td>
                      <td>
                        <ScorePill score={item.score} />
                      </td>
                      <td>{item.stageName}</td>
                      <td>
                        <Collapsible title="Reasons / Gaps">
                          <strong>Reasons</strong>
                          <ul>
                            {item.reasons.map((reason, i) => (
                              <li key={`r-${i}`}>{reason}</li>
                            ))}
                          </ul>
                          <strong>Gaps</strong>
                          <ul>
                            {item.gaps.map((gap, i) => (
                              <li key={`g-${i}`}>{gap}</li>
                            ))}
                          </ul>
                        </Collapsible>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </PanelCard>

        <PanelCard
          title="Applications"
          actions={
            <div className="row-actions">
              {!applicationsForbidden ? <button type="button" onClick={() => void loadApplications()} disabled={applicationsState.loading}>Retry</button> : null}
              <button type="button" onClick={() => setAddModalOpen(true)} disabled={applicationsForbidden}>Add candidate to job</button>
            </div>
          }
        >
          <div id="applications" />
          {applicationsState.loading ? <LoadingSkeleton lines={6} /> : null}
          {applicationsForbidden ? <ForbiddenState message="Applications panelini görüntülemek için yetkin yok." /> : null}
          {!applicationsForbidden && applicationsState.error ? (
            <ErrorState message={getErrorMessage(applicationsState.error, "Applications yüklenemedi")} onRetry={() => void loadApplications()} />
          ) : null}
          {!applicationsForbidden && appActionError ? <p className="inline-error">{appActionError}</p> : null}

          {!applicationsForbidden && !applicationsState.loading && !applicationsState.error && applicationsState.data.length === 0 ? (
            <EmptyState title="Başvuru yok" message="Bu ilana henüz başvuru eklenmemiş." actionLabel="Add candidate to job" onAction={() => setAddModalOpen(true)} />
          ) : null}

          {!applicationsForbidden && applicationsState.data.length > 0 ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Candidate</th>
                    <th>Stage</th>
                    <th>Status</th>
                    <th>Interview</th>
                  </tr>
                </thead>
                <tbody>
                  {applicationsState.data.map((app) => (
                    <tr key={app.id}>
                      <td>
                        <Link to={buildCandidateUrl(app.candidateId, jobId)}>{app.candidateFullName}</Link>
                        <br />
                        <small>{app.candidateId}</small>
                      </td>
                      <td>
                        <StageSelect
                          value={stageInputByApp[app.id] ?? app.stageId}
                          options={stageOptions}
                          disabled={applicationsForbidden}
                          onChange={(next) => setStageInputByApp((state) => ({ ...state, [app.id]: next }))}
                        />
                        <button type="button" onClick={() => void onUpdateStage(app.id)} disabled={applicationsForbidden}>
                          Update Stage
                        </button>
                      </td>
                      <td>{app.status}</td>
                      <td>
                        {(() => {
                          const sessionId = startedSessionByApp[app.id] ?? app.sessionId ?? app.interviewSessionId ?? app.interviewId ?? null;
                          return (
                            <div className="row-actions">
                              {sessionId ? (
                                <Link to={buildInterviewUrl(sessionId, jobId, app.candidateId, app.id)}>Open Interview</Link>
                              ) : (
                                <button
                                  type="button"
                                  onClick={() => void onStartInterview(app.id, app.candidateId)}
                                  disabled={applicationsForbidden || startingInterviewAppId === app.id}
                                >
                                  {startingInterviewAppId === app.id ? "Starting..." : "Start Interview"}
                                </button>
                              )}
                            </div>
                          );
                        })()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </PanelCard>
      </div>

      <ConfirmDialog
        open={publishOpen}
        title="Publish Job"
        confirmText="Publish"
        onCancel={() => setPublishOpen(false)}
        onConfirm={() => void onPublish()}
      >
        <p>İlan yayınlansın mı?</p>
      </ConfirmDialog>

      <ConfirmDialog open={addModalOpen} title="Add Candidate" onCancel={() => setAddModalOpen(false)} onConfirm={() => void onAddApplication()} confirmText="Add">
        {candidatesState.loading ? <LoadingSkeleton lines={3} /> : null}
        {candidatesForbidden ? <ForbiddenState message="Aday listesi için yetkin yok." /> : null}
        {!candidatesForbidden && candidatesState.error ? (
          <ErrorState message={getErrorMessage(candidatesState.error, "Aday listesi alınamadı")} onRetry={() => void loadCandidates()} />
        ) : null}
        <CandidateSelect
          candidates={candidatesState.data}
          value={candidateIdToAdd}
          onChange={setCandidateIdToAdd}
          disabled={candidatesState.loading || candidatesForbidden}
        />
      </ConfirmDialog>

      {!weightsState.forbidden ? <JsonErrorBox error={weightsState.error} /> : null}
    </section>
  );
}



