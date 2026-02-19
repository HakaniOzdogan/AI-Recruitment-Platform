import { AxiosError } from "axios";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { MoreHorizontal } from "lucide-react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { Candidate, CandidateCreatePayload, createCandidate, getCandidates } from "../api/endpoints/candidates";
import { canManageCandidates } from "../auth/capabilities";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { JsonErrorBox } from "../components/JsonErrorBox";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { TableSkeleton } from "../components/TableSkeleton";

const initialForm: CandidateCreatePayload = {
  fullName: "",
  email: null,
  phone: null,
  source: null
};

export function CandidatesPage(): JSX.Element {
  const allowManage = canManageCandidates();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState<Candidate[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<CandidateCreatePayload>(initialForm);
  const [lastError, setLastError] = useState<unknown>(null);
  const [searchText, setSearchText] = useState("");

  const filteredItems = useMemo(() => {
    const q = searchText.trim().toLowerCase();
    return [...items]
      .filter((item) => {
        if (!q) {
          return true;
        }
        return `${item.fullName} ${item.email ?? ""} ${item.source ?? ""}`.toLowerCase().includes(q);
      })
      .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt));
  }, [items, searchText]);

  async function loadCandidates() {
    setLoading(true);
    setLastError(null);
    try {
      const data = await getCandidates();
      setItems(data);
    } catch (error) {
      setLastError(error);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadCandidates();
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
    setSaving(true);
    setLastError(null);

    try {
      await createCandidate({
        fullName: form.fullName.trim(),
        email: form.email?.trim() || null,
        phone: form.phone?.trim() || null,
        source: form.source?.trim() || null
      });

      setForm(initialForm);
      setShowCreate(false);
      clearCreateQuery();
      await loadCandidates();
    } catch (error) {
      setLastError(error);
    } finally {
      setSaving(false);
    }
  }

  const plainError =
    lastError instanceof AxiosError && lastError.response?.status !== 400
      ? (lastError.response?.data as { message?: string } | undefined)?.message ?? "İşlem başarısız"
      : null;

  return (
    <section className="page-stack">
      <PageHeader
        title="Candidates"
        subtitle={`Toplam ${items.length} aday`}
        actions={
          <>
            <button type="button" className="button secondary" onClick={() => void loadCandidates()} disabled={loading}>
              Yenile
            </button>
            {allowManage ? (
              <button type="button" className="button" onClick={() => setShowCreate(true)}>
                Create Candidate
              </button>
            ) : null}
          </>
        }
      />

      <div className="card modern-card">
        <SearchInput value={searchText} onChange={setSearchText} placeholder="Search name / email / source" />
      </div>

      {plainError ? <ErrorState message={plainError} onRetry={() => void loadCandidates()} /> : null}
      <JsonErrorBox error={lastError} />

      {loading ? <TableSkeleton rows={6} /> : null}

      {!loading && filteredItems.length === 0 ? (
        <EmptyState
          title="No candidates yet"
          message={allowManage ? "İlk adayı oluşturarak devam et." : "Görüntüleyebileceğin aday bulunamadı."}
          actionLabel={allowManage ? "Create Candidate" : undefined}
          onAction={allowManage ? () => setShowCreate(true) : undefined}
        />
      ) : null}

      {!loading && filteredItems.length > 0 ? (
        <div className="card modern-card">
          <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Updated</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredItems.map((item) => (
                <tr key={item.id} onClick={() => navigate(`/candidates/${item.id}`)} style={{ cursor: "pointer" }}>
                  <td>
                    <div className="row-actions">
                      <span className="avatar-placeholder" aria-hidden>
                        {item.fullName.slice(0, 1).toUpperCase()}
                      </span>
                      <span>{item.fullName}</span>
                    </div>
                  </td>
                  <td>{item.email ?? "-"}</td>
                  <td>{new Date(item.createdAt).toLocaleString()}</td>
                  <td onClick={(e) => e.stopPropagation()}>
                    <details className="row-menu">
                      <summary aria-label="Row actions">
                        <MoreHorizontal size={16} />
                      </summary>
                      <div className="row-menu-content">
                        <Link to={`/candidates/${item.id}`}>View</Link>
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
        title="Create Candidate"
        showActions={false}
        onCancel={() => {
          setShowCreate(false);
          setForm(initialForm);
          clearCreateQuery();
        }}
        onConfirm={() => {}}
      >
        <form onSubmit={handleCreate} className="form-grid">
          <label>
            Full Name
            <input value={form.fullName} onChange={(e) => setForm((s) => ({ ...s, fullName: e.target.value }))} required />
          </label>
          <label>
            Email
            <input value={form.email ?? ""} onChange={(e) => setForm((s) => ({ ...s, email: e.target.value }))} />
          </label>
          <label>
            Phone
            <input value={form.phone ?? ""} onChange={(e) => setForm((s) => ({ ...s, phone: e.target.value }))} />
          </label>
          <div className="row-actions">
            <button type="button" className="button secondary" onClick={() => setShowCreate(false)}>
              Cancel
            </button>
            <button type="submit" className="button" disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </button>
          </div>
        </form>
      </ConfirmDialog>
    </section>
  );
}
