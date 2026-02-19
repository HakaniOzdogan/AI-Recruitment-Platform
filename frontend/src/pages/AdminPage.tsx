import { AxiosError } from "axios";
import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  AdminPipelineStage,
  AdminRole,
  AdminUser,
  createAdminPipelineStage,
  createAdminRole,
  createAdminUser,
  deleteAdminPipelineStage,
  deleteAdminRole,
  deleteAdminUser,
  getAdminPermissions,
  getAdminPipelineStages,
  getAdminRoles,
  getAdminUsers,
  updateAdminPipelineStage,
  updateAdminRole,
  updateAdminUser
} from "../api/endpoints/admin";
import { canAccessAdminPanel } from "../auth/capabilities";
import { EmptyState } from "../components/EmptyState";
import { ErrorState } from "../components/ErrorState";
import { JsonErrorBox } from "../components/JsonErrorBox";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

type UserFormState = {
  fullName: string;
  email: string;
  password: string;
  status: "Active" | "Disabled";
  roleIds: string[];
};

type RoleFormState = {
  name: string;
  permissionKeys: string[];
};

type StageFormState = {
  name: string;
  order: string;
  isTerminal: boolean;
};

const initialUserForm: UserFormState = {
  fullName: "",
  email: "",
  password: "",
  status: "Active",
  roleIds: []
};

const initialRoleForm: RoleFormState = {
  name: "",
  permissionKeys: []
};

const initialStageForm: StageFormState = {
  name: "",
  order: "",
  isTerminal: false
};

function getMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    return (error.response?.data as { message?: string } | undefined)?.message ?? "İşlem başarısız";
  }

  return "İşlem başarısız";
}

export function AdminPage(): JSX.Element {
  const canAccess = canAccessAdminPanel();

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [stages, setStages] = useState<AdminPipelineStage[]>([]);
  const [permissionKeys, setPermissionKeys] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawError, setRawError] = useState<unknown>(null);

  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editingRoleId, setEditingRoleId] = useState<string | null>(null);
  const [editingStageId, setEditingStageId] = useState<string | null>(null);

  const [userForm, setUserForm] = useState<UserFormState>(initialUserForm);
  const [roleForm, setRoleForm] = useState<RoleFormState>(initialRoleForm);
  const [stageForm, setStageForm] = useState<StageFormState>(initialStageForm);

  const roleOptions = useMemo(() => roles.map((x) => ({ id: x.id, label: x.name })), [roles]);

  async function loadAll() {
    setLoading(true);
    setError(null);
    setRawError(null);

    try {
      const [nextUsers, nextRoles, nextStages, nextPermissionKeys] = await Promise.all([
        getAdminUsers(),
        getAdminRoles(),
        getAdminPipelineStages(),
        getAdminPermissions()
      ]);
      setUsers(nextUsers);
      setRoles(nextRoles);
      setStages(nextStages);
      setPermissionKeys(nextPermissionKeys);
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadAll();
  }, []);

  function resetUserForm() {
    setEditingUserId(null);
    setUserForm(initialUserForm);
  }

  function resetRoleForm() {
    setEditingRoleId(null);
    setRoleForm(initialRoleForm);
  }

  function resetStageForm() {
    setEditingStageId(null);
    setStageForm(initialStageForm);
  }

  async function onUserSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setRawError(null);
    try {
      if (editingUserId) {
        await updateAdminUser(editingUserId, {
          fullName: userForm.fullName,
          email: userForm.email,
          password: userForm.password.trim() || undefined,
          status: userForm.status,
          roleIds: userForm.roleIds
        });
      } else {
        await createAdminUser({
          fullName: userForm.fullName,
          email: userForm.email,
          password: userForm.password,
          status: userForm.status,
          roleIds: userForm.roleIds
        });
      }
      resetUserForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  async function onRoleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setRawError(null);
    try {
      if (editingRoleId) {
        await updateAdminRole(editingRoleId, roleForm);
      } else {
        await createAdminRole(roleForm);
      }
      resetRoleForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  async function onStageSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setRawError(null);
    try {
      const payload = {
        name: stageForm.name,
        order: Number(stageForm.order),
        isTerminal: stageForm.isTerminal
      };

      if (editingStageId) {
        await updateAdminPipelineStage(editingStageId, payload);
      } else {
        await createAdminPipelineStage(payload);
      }
      resetStageForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteUser(id: string) {
    if (!window.confirm("Kullanıcı silinsin mi?")) return;
    setSaving(true);
    try {
      await deleteAdminUser(id);
      if (editingUserId === id) resetUserForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteRole(id: string) {
    if (!window.confirm("Rol silinsin mi?")) return;
    setSaving(true);
    try {
      await deleteAdminRole(id);
      if (editingRoleId === id) resetRoleForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteStage(id: string) {
    if (!window.confirm("Pipeline stage silinsin mi?")) return;
    setSaving(true);
    try {
      await deleteAdminPipelineStage(id);
      if (editingStageId === id) resetStageForm();
      await loadAll();
    } catch (caught) {
      setError(getMessage(caught));
      setRawError(caught);
    } finally {
      setSaving(false);
    }
  }

  if (!canAccess) {
    return (
      <section className="page-stack">
        <PageHeader title="Admin Panel" subtitle="Bu alana erişim için Admin rolü gerekir." />
        <ErrorState message="Bu panel için yetkin yok." />
      </section>
    );
  }

  return (
    <section className="page-stack">
      <PageHeader
        title="Admin Panel"
        subtitle="Kullanıcı, rol/permission ve pipeline stage yönetimi"
        actions={
          <button className="button secondary" type="button" onClick={() => void loadAll()} disabled={loading || saving}>
            Yenile
          </button>
        }
      />

      {error ? <ErrorState message={error} onRetry={() => void loadAll()} /> : null}
      <JsonErrorBox error={rawError} />

      {!loading && (
        <div className="dashboard-grid" style={{ gridTemplateColumns: "1fr" }}>
          <div className="card modern-card">
            <h3>Kullanıcılar</h3>
            {users.length === 0 ? (
              <EmptyState title="Kullanıcı yok" message="İlk kullanıcıyı aşağıdaki formdan ekleyebilirsin." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Ad Soyad</th>
                      <th>Email</th>
                      <th>Durum</th>
                      <th>Roller</th>
                      <th>İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.map((user) => (
                      <tr key={user.id}>
                        <td>{user.fullName}</td>
                        <td>{user.email}</td>
                        <td>
                          <StatusBadge label={user.status} />
                        </td>
                        <td>{user.roles.join(", ") || "-"}</td>
                        <td className="row-actions">
                          <button
                            type="button"
                            className="button secondary"
                            onClick={() => {
                              setEditingUserId(user.id);
                              setUserForm({
                                fullName: user.fullName,
                                email: user.email,
                                password: "",
                                status: user.status,
                                roleIds: roles.filter((r) => user.roles.includes(r.name)).map((r) => r.id)
                              });
                            }}
                            disabled={saving}
                          >
                            Düzenle
                          </button>
                          <button type="button" onClick={() => void onDeleteUser(user.id)} disabled={saving}>
                            Sil
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <hr className="separator" />
            <form className="form-grid" onSubmit={onUserSubmit}>
              <h4>{editingUserId ? "Kullanıcı Güncelle" : "Yeni Kullanıcı"}</h4>
              <label>
                Ad Soyad
                <input
                  value={userForm.fullName}
                  onChange={(e) => setUserForm((s) => ({ ...s, fullName: e.target.value }))}
                  disabled={saving}
                />
              </label>
              <label>
                Email
                <input value={userForm.email} onChange={(e) => setUserForm((s) => ({ ...s, email: e.target.value }))} disabled={saving} />
              </label>
              <label>
                {editingUserId ? "Yeni Şifre (opsiyonel)" : "Şifre"}
                <input
                  type="password"
                  value={userForm.password}
                  onChange={(e) => setUserForm((s) => ({ ...s, password: e.target.value }))}
                  disabled={saving}
                />
              </label>
              <label>
                Durum
                <select
                  value={userForm.status}
                  onChange={(e) => setUserForm((s) => ({ ...s, status: e.target.value as "Active" | "Disabled" }))}
                  disabled={saving}
                >
                  <option value="Active">Active</option>
                  <option value="Disabled">Disabled</option>
                </select>
              </label>

              <div>
                <strong>Roller</strong>
                <div className="row-actions" style={{ marginTop: 8 }}>
                  {roleOptions.map((role) => (
                    <label key={role.id} style={{ marginBottom: 0, flexDirection: "row", alignItems: "center", gap: 6 }}>
                      <input
                        type="checkbox"
                        checked={userForm.roleIds.includes(role.id)}
                        onChange={(e) =>
                          setUserForm((s) => ({
                            ...s,
                            roleIds: e.target.checked ? [...s.roleIds, role.id] : s.roleIds.filter((x) => x !== role.id)
                          }))
                        }
                        disabled={saving}
                        style={{ width: "auto" }}
                      />
                      {role.label}
                    </label>
                  ))}
                </div>
              </div>

              <div className="row-actions">
                <button type="submit" disabled={saving}>
                  {saving ? "Kaydediliyor..." : editingUserId ? "Güncelle" : "Ekle"}
                </button>
                {editingUserId ? (
                  <button type="button" className="button secondary" onClick={resetUserForm} disabled={saving}>
                    Vazgeç
                  </button>
                ) : null}
              </div>
            </form>
          </div>

          <div className="card modern-card">
            <h3>Roller ve Permission</h3>
            {roles.length === 0 ? (
              <EmptyState title="Rol yok" message="İlk rolü aşağıdaki formdan ekleyebilirsin." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Rol</th>
                      <th>Permission</th>
                      <th>Kullanıcı Sayısı</th>
                      <th>İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {roles.map((role) => (
                      <tr key={role.id}>
                        <td>{role.name}</td>
                        <td>{role.permissions.join(", ") || "-"}</td>
                        <td>{role.userCount}</td>
                        <td className="row-actions">
                          <button
                            type="button"
                            className="button secondary"
                            onClick={() => {
                              setEditingRoleId(role.id);
                              setRoleForm({ name: role.name, permissionKeys: role.permissions });
                            }}
                            disabled={saving}
                          >
                            Düzenle
                          </button>
                          <button type="button" onClick={() => void onDeleteRole(role.id)} disabled={saving}>
                            Sil
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <hr className="separator" />
            <form className="form-grid" onSubmit={onRoleSubmit}>
              <h4>{editingRoleId ? "Rol Güncelle" : "Yeni Rol"}</h4>
              <label>
                Rol Adı
                <input value={roleForm.name} onChange={(e) => setRoleForm((s) => ({ ...s, name: e.target.value }))} disabled={saving} />
              </label>

              <div>
                <strong>Permission</strong>
                <div className="row-actions" style={{ marginTop: 8 }}>
                  {permissionKeys.map((permission) => (
                    <label key={permission} style={{ marginBottom: 0, flexDirection: "row", alignItems: "center", gap: 6 }}>
                      <input
                        type="checkbox"
                        checked={roleForm.permissionKeys.includes(permission)}
                        onChange={(e) =>
                          setRoleForm((s) => ({
                            ...s,
                            permissionKeys: e.target.checked
                              ? [...s.permissionKeys, permission]
                              : s.permissionKeys.filter((x) => x !== permission)
                          }))
                        }
                        disabled={saving}
                        style={{ width: "auto" }}
                      />
                      {permission}
                    </label>
                  ))}
                </div>
              </div>

              <div className="row-actions">
                <button type="submit" disabled={saving}>
                  {saving ? "Kaydediliyor..." : editingRoleId ? "Güncelle" : "Ekle"}
                </button>
                {editingRoleId ? (
                  <button type="button" className="button secondary" onClick={resetRoleForm} disabled={saving}>
                    Vazgeç
                  </button>
                ) : null}
              </div>
            </form>
          </div>

          <div className="card modern-card">
            <h3>Pipeline Stage</h3>
            {stages.length === 0 ? (
              <EmptyState title="Stage yok" message="İlk stage’i aşağıdaki formdan ekleyebilirsin." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Ad</th>
                      <th>Order</th>
                      <th>Terminal</th>
                      <th>Application</th>
                      <th>İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stages.map((stage) => (
                      <tr key={stage.id}>
                        <td>{stage.name}</td>
                        <td>{stage.order}</td>
                        <td>{stage.isTerminal ? "Evet" : "Hayır"}</td>
                        <td>{stage.applicationCount}</td>
                        <td className="row-actions">
                          <button
                            type="button"
                            className="button secondary"
                            onClick={() => {
                              setEditingStageId(stage.id);
                              setStageForm({ name: stage.name, order: String(stage.order), isTerminal: stage.isTerminal });
                            }}
                            disabled={saving}
                          >
                            Düzenle
                          </button>
                          <button type="button" onClick={() => void onDeleteStage(stage.id)} disabled={saving}>
                            Sil
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <hr className="separator" />
            <form className="form-grid" onSubmit={onStageSubmit}>
              <h4>{editingStageId ? "Stage Güncelle" : "Yeni Stage"}</h4>
              <label>
                Stage Adı
                <input value={stageForm.name} onChange={(e) => setStageForm((s) => ({ ...s, name: e.target.value }))} disabled={saving} />
              </label>
              <label>
                Order
                <input
                  type="number"
                  min={1}
                  value={stageForm.order}
                  onChange={(e) => setStageForm((s) => ({ ...s, order: e.target.value }))}
                  disabled={saving}
                />
              </label>
              <label style={{ marginBottom: 0, flexDirection: "row", alignItems: "center", gap: 8 }}>
                <input
                  type="checkbox"
                  checked={stageForm.isTerminal}
                  onChange={(e) => setStageForm((s) => ({ ...s, isTerminal: e.target.checked }))}
                  disabled={saving}
                  style={{ width: "auto" }}
                />
                Terminal Stage
              </label>

              <div className="row-actions">
                <button type="submit" disabled={saving}>
                  {saving ? "Kaydediliyor..." : editingStageId ? "Güncelle" : "Ekle"}
                </button>
                {editingStageId ? (
                  <button type="button" className="button secondary" onClick={resetStageForm} disabled={saving}>
                    Vazgeç
                  </button>
                ) : null}
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  );
}
