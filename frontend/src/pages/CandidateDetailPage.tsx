import { AxiosError } from "axios";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { AiEvaluationReport, getAiEvalLatest, listJobs, runAiEval } from "../api/endpoints/ai";
import {
  Candidate,
  CandidateProfile,
  CvUploadResponse,
  getCandidate,
  getCandidateProfile,
  parseCv,
  uploadCandidateCv
} from "../api/endpoints/candidates";
import { Job } from "../api/endpoints/jobs";
import { EmptyState } from "../components/EmptyState";
import { FileUploadBox } from "../components/FileUploadBox";
import { JsonErrorBox } from "../components/JsonErrorBox";
import { KeyValueList } from "../components/KeyValueList";
import { LoadingSpinner } from "../components/LoadingSpinner";
import { ReportCard } from "../components/ReportCard";
import { TagList } from "../components/TagList";
import { pushToast } from "../store/uiFeedback";

type ParseState = "idle" | "loading" | "done" | "error";
type EvalState = {
  loading: boolean;
  error: unknown;
  forbidden: boolean;
  consentRequired: boolean;
  data: AiEvaluationReport | null;
};

function parseJsonArray(value: string): string[] {
  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) {
      return parsed.map((item) => String(item));
    }

    return [];
  } catch {
    return [];
  }
}

function parseExperienceEntries(value: string): Array<{ key: string; value: string }> {
  try {
    const parsed = JSON.parse(value);
    if (!Array.isArray(parsed)) {
      return [];
    }

    return parsed.map((row, index) => {
      if (!row || typeof row !== "object") {
        return { key: `experience-${index + 1}`, value: String(row) };
      }

      const item = row as Record<string, unknown>;
      const title = String(item.title ?? item.role ?? `experience-${index + 1}`);
      const company = String(item.company ?? item.organization ?? "-");
      return { key: title, value: company };
    });
  } catch {
    return [];
  }
}

function getCorrelationRef(error: AxiosError): string {
  const enriched = error as AxiosError & { correlationId?: string };
  const direct = enriched.correlationId;
  if (direct) {
    return ` Ref: ${direct}`;
  }

  const headerValue = error.response?.headers?.["x-correlation-id"];
  if (typeof headerValue === "string" && headerValue.length > 0) {
    return ` Ref: ${headerValue}`;
  }

  return "";
}

export function CandidateDetailPage(): JSX.Element {
  const { id } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const candidateId = id ?? "";
  const queryJobId = searchParams.get("jobId") ?? "";

  const [candidate, setCandidate] = useState<Candidate | null>(null);
  const [profile, setProfile] = useState<CandidateProfile | null>(null);
  const [profileMissing, setProfileMissing] = useState(false);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [selectedJobId, setSelectedJobId] = useState("");
  const [latestCv, setLatestCv] = useState<CvUploadResponse | null>(null);
  const [parseState, setParseState] = useState<ParseState>("idle");
  const [loading, setLoading] = useState(false);
  const [runEvalLoading, setRunEvalLoading] = useState(false);
  const [lastError, setLastError] = useState<unknown>(null);
  const [profileForbidden, setProfileForbidden] = useState(false);
  const [evalState, setEvalState] = useState<EvalState>({
    loading: false,
    error: null,
    forbidden: false,
    consentRequired: false,
    data: null
  });

  const evalAbortRef = useRef<AbortController | null>(null);
  const selectedJobRef = useRef("");

  const skills = useMemo(() => (profile ? parseJsonArray(profile.skillsJson) : []), [profile]);
  const educationEntries = useMemo(() => (profile ? parseExperienceEntries(profile.educationJson) : []), [profile]);
  const experienceEntries = useMemo(() => (profile ? parseExperienceEntries(profile.experienceJson) : []), [profile]);

  async function refreshProfile() {
    if (!candidateId) {
      return;
    }

    try {
      const profileData = await getCandidateProfile(candidateId);
      setProfile(profileData);
      setProfileMissing(false);
      setProfileForbidden(false);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 403) {
        setProfileForbidden(true);
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 404) {
        setProfile(null);
        setProfileMissing(true);
        return;
      }

      throw error;
    }
  }

  async function loadCandidateContext() {
    if (!candidateId) {
      return;
    }

    setLoading(true);
    setLastError(null);
    try {
      const [candidateData, jobsData] = await Promise.all([getCandidate(candidateId), listJobs()]);
      setCandidate(candidateData);
      setJobs(jobsData);

      if (jobsData.length > 0) {
        const hasQueryJob = queryJobId.length > 0 && jobsData.some((job) => job.id === queryJobId);
        if (hasQueryJob) {
          setSelectedJobId(queryJobId);
        } else {
          const hasCurrent = selectedJobId.length > 0 && jobsData.some((job) => job.id === selectedJobId);
          if (!hasCurrent) {
            setSelectedJobId(jobsData[0].id);
          }
        }
      } else {
        setSelectedJobId("");
      }

      await refreshProfile();
    } catch (error) {
      setLastError(error);
    } finally {
      setLoading(false);
    }
  }

  async function fetchLatestEval(jobId: string) {
    if (!candidateId || !jobId) {
      setEvalState({ loading: false, error: null, forbidden: false, consentRequired: false, data: null });
      return;
    }

    setEvalState({ loading: true, error: null, forbidden: false, consentRequired: false, data: null });
    evalAbortRef.current?.abort();
    const controller = new AbortController();
    evalAbortRef.current = controller;

    try {
      const report = await getAiEvalLatest(jobId, candidateId, controller.signal);
      if (selectedJobRef.current !== jobId) {
        return;
      }
      setEvalState({ loading: false, error: null, forbidden: false, consentRequired: false, data: report });
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      if (selectedJobRef.current !== jobId) {
        return;
      }

      if (error instanceof AxiosError && error.response?.status === 404) {
        setEvalState({ loading: false, error: null, forbidden: false, consentRequired: false, data: null });
      } else if (error instanceof AxiosError && error.response?.status === 403) {
        setEvalState({ loading: false, error: null, forbidden: true, consentRequired: false, data: null });
      } else if (error instanceof AxiosError && error.response?.status === 409) {
        setEvalState({ loading: false, error: null, forbidden: false, consentRequired: true, data: null });
      } else if (error instanceof AxiosError && error.response?.status === 429) {
        pushToast("error", "Rate limit, biraz sonra dene");
        setEvalState({ loading: false, error, forbidden: false, consentRequired: false, data: null });
      } else {
        setEvalState({ loading: false, error, forbidden: false, consentRequired: false, data: null });
      }
    }
  }

  useEffect(() => {
    void loadCandidateContext();
  }, [candidateId, queryJobId]);

  useEffect(() => {
    return () => {
      evalAbortRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    selectedJobRef.current = selectedJobId;
    void fetchLatestEval(selectedJobId);
  }, [selectedJobId]);

  async function handleUpload(file: File, onProgress: (value: number) => void) {
    if (!candidateId) {
      return;
    }

    setLastError(null);
    try {
      const uploaded = await uploadCandidateCv(candidateId, file, onProgress);
      setLatestCv(uploaded);
      pushToast("info", "CV yüklendi");
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 413) {
        pushToast("error", `Dosya çok büyük (max 10MB).${getCorrelationRef(error)}`);
        setLastError(new Error("Dosya çok büyük (max 10MB)."));
        return;
      }
      if (error instanceof AxiosError && error.response?.status === 400) {
        pushToast("error", `Geçersiz dosya.${getCorrelationRef(error)}`);
        setLastError(error);
        return;
      }
      if (error instanceof AxiosError && error.response && error.response.status >= 500) {
        pushToast("error", `Yükleme sırasında sunucu hatası.${getCorrelationRef(error)}`);
      }

      setLastError(error);
    }
  }

  async function handleParse() {
    if (!latestCv?.cvDocumentId) {
      return;
    }

    setParseState("loading");
    setLastError(null);
    try {
      await parseCv(latestCv.cvDocumentId);
      await refreshProfile();
      setParseState("done");
      pushToast("info", "CV parse tamamlandı");
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 409) {
        pushToast("error", `Parse işlemi şu anda yapılamıyor.${getCorrelationRef(error)}`);
      } else if (error instanceof AxiosError && error.response?.status === 400) {
        pushToast("error", `Geçersiz parse isteği.${getCorrelationRef(error)}`);
      } else if (error instanceof AxiosError && error.response && error.response.status >= 500) {
        pushToast("error", `Parse sırasında sunucu hatası.${getCorrelationRef(error)}`);
      }
      setLastError(error);
      setParseState("error");
    }
  }

  async function handleLoadReport() {
    await fetchLatestEval(selectedJobId);
  }

  async function handleForceEval() {
    if (!candidateId || !selectedJobId) {
      return;
    }

    setRunEvalLoading(true);
    setEvalState((state) => ({ ...state, error: null, forbidden: false, consentRequired: false }));
    evalAbortRef.current?.abort();
    const controller = new AbortController();
    evalAbortRef.current = controller;

    try {
      await runAiEval(selectedJobId, candidateId, true, controller.signal);
      await fetchLatestEval(selectedJobId);
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      if (error instanceof AxiosError && error.response?.status === 403) {
        setEvalState({ loading: false, error: null, forbidden: true, consentRequired: false, data: null });
        return;
      }

      if (error instanceof AxiosError && error.response?.status === 409) {
        setEvalState({ loading: false, error: null, forbidden: false, consentRequired: true, data: null });
        return;
      }

      if (error instanceof AxiosError && error.response?.status === 429) {
        pushToast("error", "Rate limit, biraz sonra dene");
      }

      setEvalState((state) => ({ ...state, error }));
    } finally {
      setRunEvalLoading(false);
    }
  }

  const plainError =
    lastError instanceof Error && !(lastError instanceof AxiosError)
      ? lastError.message
      : lastError instanceof AxiosError && lastError.response?.status !== 400
        ? (lastError.response?.data as { message?: string } | undefined)?.message ?? "İşlem başarısız"
        : null;

  return (
    <section className="card">
      {loading ? <LoadingSpinner /> : null}

      <div className="row-actions">
        <div>
          <h1>{candidate?.fullName ?? "Candidate Detail"}</h1>
          <p>{candidate?.createdAt ? new Date(candidate.createdAt).toLocaleString() : "-"}</p>
        </div>
        <div className="row-actions">
          {queryJobId ? <Link to={`/jobs/${queryJobId}`}>← Back to Job</Link> : null}
          <Link to="/candidates">← Geri</Link>
          <button type="button" onClick={() => void loadCandidateContext()} disabled={loading}>
            Retry Context
          </button>
        </div>
      </div>

      {plainError ? <p className="inline-error">{plainError}</p> : null}
      <JsonErrorBox error={lastError} />

      <section className="card section-card">
        <h2>CV Upload</h2>
        <FileUploadBox onUpload={handleUpload} disabled={!candidateId} />
        {latestCv ? (
          <p>
            Son yüklenen CV: <strong>{latestCv.originalFileName}</strong> (ID: {latestCv.cvDocumentId})
          </p>
        ) : (
          <EmptyState title="CV yok" message="PDF/DOCX dosyası yükleyin." />
        )}
      </section>

      <section className="card section-card">
        <h2>CV Parse</h2>
        <button type="button" disabled={!latestCv?.cvDocumentId || parseState === "loading"} onClick={() => void handleParse()}>
          {parseState === "loading" ? "Parsing..." : "Parse CV"}
        </button>
        <button type="button" onClick={() => void refreshProfile()}>
          Refresh Profile
        </button>
        {parseState === "done" ? <p className="muted">Parse tamamlandı.</p> : null}
        {parseState === "error" ? <p className="inline-error">Parse sırasında hata oluştu.</p> : null}
      </section>

      <section className="card section-card">
        <h2>Candidate Profile</h2>
        {profileForbidden ? <p className="inline-error">Profile bölümünü görüntüleme yetkin yok.</p> : null}
        {profileMissing ? <EmptyState title="Profile yok" message="CV parse edilmedi." /> : null}

        {profile ? (
          <>
            <p>
              Summary: {profile.summary ?? "-"}
              <br />
              Experience Months: {profile.totalExperienceMonths ?? "-"}
            </p>
            <TagList tags={skills} />
            <KeyValueList title="Experience" items={experienceEntries} />
            <KeyValueList title="Education" items={educationEntries} />
          </>
        ) : null}
      </section>

      <section className="card section-card">
        <h2>AI Evaluation</h2>
        <div className="row-actions">
          <label>
            Job
            <select
              value={selectedJobId}
              onChange={(e) => {
                const next = e.target.value;
                setSelectedJobId(next);
                if (next) {
                  setSearchParams({ jobId: next });
                } else {
                  setSearchParams({});
                }
              }}
            >
              {jobs.map((job) => (
                <option key={job.id} value={job.id}>
                  {job.title}
                </option>
              ))}
            </select>
          </label>
          <button type="button" onClick={() => void handleLoadReport()} disabled={!selectedJobId || evalState.loading || evalState.forbidden}>
            Load Report
          </button>
          <button
            type="button"
            onClick={() => void handleForceEval()}
            disabled={!selectedJobId || evalState.loading || runEvalLoading || evalState.forbidden}
          >
            Force Evaluate
          </button>
          <button type="button" onClick={() => void handleLoadReport()} disabled={!selectedJobId || evalState.loading}>
            Retry Eval
          </button>
        </div>

        {evalState.loading ? <LoadingSpinner label="Rapor yükleniyor..." /> : null}
        {evalState.forbidden ? <p className="inline-error">AI evaluation için yetkin yok.</p> : null}
        {evalState.consentRequired ? <p className="inline-error">Aday rızası gerekli</p> : null}
        {!evalState.forbidden && !evalState.consentRequired ? <JsonErrorBox error={evalState.error} /> : null}

        {evalState.data ? (
          <ReportCard report={evalState.data} />
        ) : (
          <EmptyState title="Rapor yok" message="Henüz rapor yok. Force Evaluate ile oluşturabilirsin." />
        )}
      </section>
    </section>
  );
}
