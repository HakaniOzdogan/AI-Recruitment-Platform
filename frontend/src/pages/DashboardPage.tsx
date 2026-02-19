import { ArrowRight, Briefcase, MessageCircle, UserPlus } from "lucide-react";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getCandidates } from "../api/endpoints/candidates";
import { getJobs } from "../api/endpoints/jobs";
import { canApplyAndInterview, canManageCandidates, canManageJobs } from "../auth/capabilities";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { LoadingSkeleton } from "../components/LoadingSkeleton";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

function CtaCard({
  to,
  title,
  desc,
  icon,
  disabled
}: {
  to?: string;
  title: string;
  desc: string;
  icon: JSX.Element;
  disabled?: boolean;
}): JSX.Element {
  const content = (
    <div className={`card modern-card cta-card ${disabled ? "is-disabled" : ""}`} title={disabled ? "Bu aksiyon bu rolde kapali." : undefined}>
      <div className="row-actions">
        <div className="icon-chip">{icon}</div>
        <ArrowRight size={16} />
      </div>
      <h3>{title}</h3>
      <p className="muted">{desc}</p>
    </div>
  );

  if (!to || disabled) {
    return content;
  }

  return <Link to={to}>{content}</Link>;
}

export function DashboardPage(): JSX.Element {
  const allowJobCreate = canManageJobs();
  const allowCandidateCreate = canManageCandidates();
  const allowInterviewFlow = canApplyAndInterview();

  const [jobsCount, setJobsCount] = useState<number | null>(null);
  const [candidatesCount, setCandidatesCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  async function loadSummary() {
    setLoading(true);
    setLoadError(null);
    try {
      const [jobs, candidates] = await Promise.all([getJobs(), getCandidates()]);
      setJobsCount(jobs.length);
      setCandidatesCount(candidates.length);
    } catch {
      setLoadError("Ozet verisi alinamadi.");
      setJobsCount(null);
      setCandidatesCount(null);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSummary();
  }, []);

  return (
    <section className="page-stack">
      <PageHeader title="IK Otomasyon" subtitle="Dogru ise alim kararlari, hizli ve guvenli surec." />

      {loading ? <LoadingSkeleton lines={4} /> : null}
      {loadError ? <ErrorState message={loadError} onRetry={() => void loadSummary()} /> : null}

      {!loading && !loadError ? (
        <>
          <div className="card modern-card">
            <div className="row-actions" style={{ justifyContent: "space-between" }}>
              <div>
                <h3 style={{ marginBottom: 8 }}>Neden Guvenilir?</h3>
                <p className="muted">Audit, RBAC ve evidence zinciri ile izlenebilir ise alim sureci.</p>
              </div>
              <div className="row-actions">
                <StatusBadge label="Audit" tone="ai" />
                <StatusBadge label="RBAC" tone="published" />
                <StatusBadge label="Evidence" tone="draft" />
              </div>
            </div>
          </div>

          <div className="dashboard-grid">
            {allowJobCreate ? <CtaCard to="/jobs?create=1" title="Create Job" desc="Yeni ilan olusturup yayinlayin." icon={<Briefcase size={18} />} /> : null}
            {allowCandidateCreate ? (
              <CtaCard to="/candidates?create=1" title="Add Candidate" desc="Aday ekleyin ve CV parse akisina gecin." icon={<UserPlus size={18} />} />
            ) : null}
            {allowInterviewFlow ? (
              <CtaCard to="/applications" title="Applications & Interview" desc="Basvurulari ve mulakat akislarini yonet." icon={<MessageCircle size={18} />} />
            ) : null}
          </div>

          <div className="dashboard-grid dashboard-stats">
            <div className="card modern-card">
              <h3>Jobs</h3>
              <p className="dashboard-metric">{jobsCount ?? "-"}</p>
            </div>
            <div className="card modern-card">
              <h3>Candidates</h3>
              <p className="dashboard-metric">{candidatesCount ?? "-"}</p>
            </div>
            <div className="card modern-card">
              <h3>Akis Durumu</h3>
              <p className="muted">Role gore aksiyonlar sadeleştirildi.</p>
            </div>
          </div>

          <div className="card modern-card">
            <h3>Quick Steps</h3>
            <ol className="quick-steps">
              <li>Basvuru kaydini ac veya mevcut basvuruyu sec.</li>
              <li>Mülakat baslat veya oturuma gir.</li>
              <li>Mesajlasma ile mulakati tamamla.</li>
              <li>Yonetici rolu score/karar asamasina gecsin.</li>
            </ol>
          </div>

          <div className="card modern-card">
            <h3>Recent Activity</h3>
            <EmptyState title="Henuz veri yok" message="Role uygun panelden ilk kaydi olusturarak baslayin." />
          </div>
        </>
      ) : null}
    </section>
  );
}
