import { useEffect, useState } from "react";
import { AxiosError } from "axios";
import { getInterviewScoreReports, InterviewScoreReportItem } from "../api/endpoints/reports";
import { canViewReports } from "../auth/capabilities";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { LoadingSkeleton } from "../components/LoadingSkeleton";
import { PageHeader } from "../components/PageHeader";
import { ScorePill } from "../components/ScorePill";
import { StatusBadge } from "../components/StatusBadge";

function toPercent(score: number | null): number {
  if (score === null || Number.isNaN(score)) {
    return 0;
  }

  return Math.round((score / 5) * 100);
}

export function ReportsPage(): JSX.Element {
  const allowed = canViewReports();
  const [items, setItems] = useState<InterviewScoreReportItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setItems(await getInterviewScoreReports(100));
    } catch (caught) {
      if (caught instanceof AxiosError) {
        if (caught.response?.status === 404) {
          setError("Reports endpoint bulunamadi. Backend'i yeniden baslatip tekrar dene.");
          return;
        }

        const message = (caught.response?.data as { message?: string } | undefined)?.message;
        setError(message ?? "Raporlar yüklenemedi.");
        return;
      }

      setError("Raporlar yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!allowed) {
      return;
    }

    void load();
  }, [allowed]);

  if (!allowed) {
    return (
      <section className="page-stack">
        <PageHeader title="Reports" subtitle="Bu alan için yetkin yok." />
        <ErrorState message="Rapor ekranı sadece Admin / HR / HiringManager rolleri için açıktır." />
      </section>
    );
  }

  return (
    <section className="page-stack">
      <PageHeader
        title="Interview Reports"
        subtitle={`Toplam ${items.length} scorecard`}
        actions={
          <button type="button" className="button secondary" onClick={() => void load()} disabled={loading}>
            Yenile
          </button>
        }
      />

      {loading ? <LoadingSkeleton lines={6} /> : null}
      {error ? <ErrorState message={error} onRetry={() => void load()} /> : null}

      {!loading && !error && items.length === 0 ? <EmptyState title="Rapor yok" message="Henüz scorecard oluşturulmamış." /> : null}

      {!loading && !error && items.length > 0 ? (
        <div className="card modern-card">
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Candidate</th>
                  <th>Job</th>
                  <th>Stage</th>
                  <th>Overall</th>
                  <th>Detay</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.scorecardId}>
                    <td>{item.candidateFullName}</td>
                    <td>{item.jobTitle}</td>
                    <td>{item.stageName}</td>
                    <td>{item.overallScore === null ? <StatusBadge label="N/A" tone="insufficient" /> : <ScorePill score={toPercent(item.overallScore)} />}</td>
                    <td>
                      <details className="row-menu" style={{ minWidth: 300 }}>
                        <summary>Göster</summary>
                        <div className="row-menu-content" style={{ position: "static", boxShadow: "none", border: "0", padding: 0 }}>
                          {item.criteria.length === 0 ? <p className="muted">Kriter puanı yok.</p> : null}
                          {item.criteria.map((criterion) => (
                            <div key={`${item.scorecardId}-${criterion.criterionKey}`} className="card" style={{ marginBottom: 8 }}>
                              <div className="row-actions" style={{ justifyContent: "space-between" }}>
                                <strong>{criterion.criterionKey}</strong>
                                <StatusBadge
                                  label={criterion.status === "SCORED" ? String(criterion.score ?? "-") : "Insufficient"}
                                  tone={criterion.status === "SCORED" ? "published" : "insufficient"}
                                />
                              </div>
                              <p className="muted">{criterion.rationale ?? "-"}</p>
                              <ul>
                                {criterion.evidenceQuotes.map((e, idx) => (
                                  <li key={`${item.scorecardId}-${criterion.criterionKey}-e-${idx}`}>
                                    "{e.quote}" ({e.relatedTo})
                                  </li>
                                ))}
                              </ul>
                            </div>
                          ))}
                        </div>
                      </details>
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
