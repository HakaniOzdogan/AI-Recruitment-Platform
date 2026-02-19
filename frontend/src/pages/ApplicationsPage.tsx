import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Application, getApplications, startInterview } from "../api/endpoints/applications";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";
import { TableSkeleton } from "../components/TableSkeleton";

export function ApplicationsPage(): JSX.Element {
  const navigate = useNavigate();
  const [items, setItems] = useState<Application[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [startingId, setStartingId] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getApplications();
      setItems(data);
    } catch {
      setError("Başvurular yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onStartOrOpenInterview(item: Application) {
    if (item.interviewSessionId) {
      navigate(`/interviews/${item.interviewSessionId}?appId=${item.id}&jobId=${item.jobId}&candidateId=${item.candidateId}`);
      return;
    }

    setStartingId(item.id);
    try {
      const started = await startInterview(item.id);
      navigate(`/interviews/${started.sessionId}?appId=${item.id}&jobId=${item.jobId}&candidateId=${item.candidateId}`);
    } catch {
      setError("Mülakat başlatılamadı.");
    } finally {
      setStartingId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        title="Applications"
        subtitle={`Toplam ${items.length} başvuru`}
        actions={
          <button type="button" className="button secondary" onClick={() => void load()} disabled={loading}>
            Yenile
          </button>
        }
      />

      {error ? <ErrorState message={error} onRetry={() => void load()} /> : null}
      {loading ? <TableSkeleton rows={6} /> : null}

      {!loading && items.length === 0 ? <EmptyState title="Başvuru yok" message="Henüz başvuru bulunmuyor." /> : null}

      {!loading && items.length > 0 ? (
        <div className="card modern-card">
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Job</th>
                  <th>Candidate</th>
                  <th>Stage</th>
                  <th>Status</th>
                  <th>Applied At</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <Link to={`/jobs/${item.jobId}`}>{item.jobTitle ?? item.jobId}</Link>
                    </td>
                    <td>
                      <Link to={`/candidates/${item.candidateId}`}>{item.candidateFullName}</Link>
                    </td>
                    <td>{item.stageName}</td>
                    <td>
                      <StatusBadge label={item.status} />
                    </td>
                    <td>{new Date(item.appliedAt).toLocaleString()}</td>
                    <td>
                      <button type="button" onClick={() => void onStartOrOpenInterview(item)} disabled={startingId === item.id}>
                        {item.interviewSessionId ? "Mülakata Git" : startingId === item.id ? "Başlatılıyor..." : "Mülakat Başlat"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </section>
  );
}
