import { AxiosError } from "axios";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { MoreHorizontal } from "lucide-react";
import { Link, useSearchParams } from "react-router-dom";
import { createJob, CreateJobPayload, getJobs, Job, publishJob } from "../api/endpoints/jobs";
import { canManageJobs } from "../auth/capabilities";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { FieldError } from "../components/FieldError";
import { FormActions } from "../components/FormActions";
import { JsonErrorBox } from "../components/JsonErrorBox";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { StatusBadge } from "../components/StatusBadge";
import { TableSkeleton } from "../components/TableSkeleton";

type JobFormState = {
  title: string;
  description: string;
  requiredSkills: string;
  niceSkills: string;
  minExperienceMonths: string;
};

const initialForm: JobFormState = {
  title: "",
  description: "",
  requiredSkills: "",
  niceSkills: "",
  minExperienceMonths: ""
};

type FormErrors = Partial<Record<keyof JobFormState, string>>;
type StatusFilter = "all" | "published" | "draft";

function toPayload(form: JobFormState): CreateJobPayload {
  return {
    title: form.title.trim(),
    description: form.description.trim(),
    requiredSkills: form.requiredSkills.split(",").map((x) => x.trim()).filter(Boolean),
    niceToHaveSkills: form.niceSkills.split(",").map((x) => x.trim()).filter(Boolean),
    minExperienceMonths: form.minExperienceMonths.trim() ? Number(form.minExperienceMonths) : null
  };
}

export function JobsPage(): JSX.Element {
  const allowManage = canManageJobs();
  const [searchParams, setSearchParams] = useSearchParams();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState<JobFormState>(initialForm);
  const [submitting, setSubmitting] = useState(false);
  const [publishTarget, setPublishTarget] = useState<Job | null>(null);
  const [lastError, setLastError] = useState<unknown>(null);
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");

  const publishedCount = useMemo(() => jobs.filter((x) => x.status === "Published").length, [jobs]);

  const filteredJobs = useMemo(() => {
    const query = searchText.trim().toLowerCase();
    return [...jobs]
      .filter((job) => {
        if (statusFilter === "published") {
          return job.status === "Published";
        }
        if (statusFilter === "draft") {
          return job.status === "Draft";
        }
        return true;
      })
      .filter((job) => {
        if (!query) {
          return true;
        }
        return `${job.title} ${job.description}`.toLowerCase().includes(query);
      })
      .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt));
  }, [jobs, searchText, statusFilter]);

  async function loadJobs() {
    setLoading(true);
    setLastError(null);
    try {
      const data = await getJobs();
      setJobs(data);
    } catch (error) {
      setLastError(error);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadJobs();
  }, []);

  useEffect(() => {
    if (allowManage && searchParams.get("create") === "1") {
      setShowCreate(true);
    }
  }, [searchParams, allowManage]);

  function clearCreateQuery() {
    if (!searchParams.has("create")) {
      return;
    }

    const next = new URLSearchParams(searchParams);
    next.delete("create");
    setSearchParams(next, { replace: true });
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    const nextErrors: FormErrors = {};
    if (!form.title.trim()) {
      nextErrors.title = "Title zorunlu";
    }
    if (!form.description.trim()) {
      nextErrors.description = "Description zorunlu";
    }

    setFormErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    setSubmitting(true);
    setLastError(null);
    setFormErrors({});

    try {
      await createJob(toPayload(form));
      setForm(initialForm);
      setShowCreate(false);
      clearCreateQuery();
      await loadJobs();
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 400) {
        const responseData = error.response.data as { errors?: Record<string, string[]> } | undefined;
        if (responseData?.errors && typeof responseData.errors === "object") {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(responseData.errors)) {
            const first = Array.isArray(messages) && messages.length > 0 ? messages[0] : null;
            if (!first) {
              continue;
            }

            const key = field.toLowerCase();
            if (key.includes("title")) {
              mapped.title = first;
            } else if (key.includes("description")) {
              mapped.description = first;
            } else if (key.includes("required")) {
              mapped.requiredSkills = first;
            } else if (key.includes("nice")) {
              mapped.niceSkills = first;
            } else if (key.includes("experience")) {
              mapped.minExperienceMonths = first;
            }
          }
          setFormErrors(mapped);
        }
      }
      setLastError(error);
    } finally {
      setSubmitting(false);
    }
  }

  async function handlePublish() {
    if (!publishTarget) {
      return;
    }

    setLastError(null);
    try {
      await publishJob(publishTarget.id);
      setPublishTarget(null);
      await loadJobs();
    } catch (error) {
      setLastError(error);
      setPublishTarget(null);
    }
  }

  const plainError =
    lastError instanceof AxiosError && lastError.response?.status !== 400
      ? (lastError.response?.data as { message?: string } | undefined)?.message ?? "İşlem başarısız"
      : null;

  return (
    <section className="page-stack">
      <PageHeader
        title="Jobs"
        subtitle={`Toplam ${jobs.length} ilan · Published ${publishedCount}`}
        actions={
          <>
            <button className="button secondary" type="button" onClick={() => void loadJobs()} disabled={loading}>
              Yenile
            </button>
            {allowManage ? (
              <button className="button" type="button" onClick={() => setShowCreate(true)}>
                Create Job
              </button>
            ) : null}
          </>
        }
      />

      <div className="card modern-card">
        <div className="row-actions">
        <SearchInput value={searchText} onChange={setSearchText} placeholder="Search title / description" />
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as StatusFilter)} style={{ maxWidth: 180 }}>
          <option value="all">All</option>
          <option value="published">Published</option>
          <option value="draft">Draft</option>
        </select>
        </div>
      </div>

      {plainError ? <ErrorState message={plainError} onRetry={() => void loadJobs()} /> : null}
      <JsonErrorBox error={lastError} />

      {loading ? <TableSkeleton rows={6} /> : null}

      {!loading && filteredJobs.length === 0 ? (
        <EmptyState
          title="No jobs yet"
          message={allowManage ? "Yeni ilan oluşturarak başlayabilirsin." : "Görüntüleyebileceğin ilan bulunamadı."}
          actionLabel={allowManage ? "Create Job" : undefined}
          onAction={allowManage ? () => setShowCreate(true) : undefined}
        />
      ) : null}

      {!loading && filteredJobs.length > 0 ? (
        <div className="card modern-card">
          <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Status</th>
                <th>Updated</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredJobs.map((job) => (
                <tr key={job.id}>
                  <td>{job.title}</td>
                  <td>
                    <StatusBadge label={job.status} />
                  </td>
                  <td>{new Date(job.publishedAt ?? job.createdAt).toLocaleString()}</td>
                  <td>
                    <details className="row-menu">
                      <summary aria-label="Row actions">
                        <MoreHorizontal size={16} />
                      </summary>
                      <div className="row-menu-content">
                        <Link to={`/jobs/${job.id}`}>View</Link>
                        <button
                          type="button"
                          onClick={() => setPublishTarget(job)}
                          disabled={!allowManage || job.status === "Published" || job.status === "Closed"}
                        >
                          Publish
                        </button>
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

      <ConfirmDialog
        open={allowManage && showCreate}
        title="Create Job"
        showActions={false}
        onCancel={() => {
          setShowCreate(false);
          setForm(initialForm);
          setFormErrors({});
          clearCreateQuery();
        }}
        onConfirm={() => {}}
      >
        <form onSubmit={handleCreate} className="form-grid">
          <label>
            Title
            <input value={form.title} onChange={(e) => setForm((s) => ({ ...s, title: e.target.value }))} disabled={submitting} />
            <FieldError message={formErrors.title} />
          </label>
          <label>
            Description
            <textarea value={form.description} onChange={(e) => setForm((s) => ({ ...s, description: e.target.value }))} rows={3} disabled={submitting} />
            <FieldError message={formErrors.description} />
          </label>
          <label>
            Required Skills (comma)
            <input value={form.requiredSkills} onChange={(e) => setForm((s) => ({ ...s, requiredSkills: e.target.value }))} disabled={submitting} />
            <FieldError message={formErrors.requiredSkills} />
          </label>
          <label>
            Nice Skills (comma)
            <input value={form.niceSkills} onChange={(e) => setForm((s) => ({ ...s, niceSkills: e.target.value }))} disabled={submitting} />
            <FieldError message={formErrors.niceSkills} />
          </label>
          <label>
            Min Experience Months
            <input
              type="number"
              min={0}
              value={form.minExperienceMonths}
              disabled={submitting}
              onChange={(e) => setForm((s) => ({ ...s, minExperienceMonths: e.target.value }))}
            />
            <FieldError message={formErrors.minExperienceMonths} />
          </label>
          <FormActions>
            <button type="button" onClick={() => setShowCreate(false)}>
              Cancel
            </button>
            <button type="submit" disabled={submitting}>
              {submitting ? "Saving..." : "Save"}
            </button>
          </FormActions>
        </form>
      </ConfirmDialog>

      <ConfirmDialog
        open={allowManage && publishTarget !== null}
        title="Publish Job"
        onCancel={() => setPublishTarget(null)}
        onConfirm={() => void handlePublish()}
        confirmText="Publish"
      >
        <p>{publishTarget?.title} ilanı yayınlansın mı?</p>
      </ConfirmDialog>
    </section>
  );
}
