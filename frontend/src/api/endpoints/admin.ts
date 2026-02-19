import { http } from "../http";

export type UserStatus = "Active" | "Disabled";

export type AdminUser = {
  id: string;
  fullName: string;
  email: string;
  status: UserStatus;
  createdAt: string;
  roles: string[];
};

export type AdminRole = {
  id: string;
  name: string;
  permissions: string[];
  userCount: number;
};

export type AdminPipelineStage = {
  id: string;
  name: string;
  order: number;
  isTerminal: boolean;
  applicationCount: number;
};

export type CreateAdminUserPayload = {
  fullName: string;
  email: string;
  password: string;
  status: UserStatus;
  roleIds: string[];
};

export type UpdateAdminUserPayload = {
  fullName: string;
  email: string;
  password?: string;
  status: UserStatus;
  roleIds: string[];
};

export type UpsertRolePayload = {
  name: string;
  permissionKeys: string[];
};

export type UpsertStagePayload = {
  name: string;
  order: number;
  isTerminal: boolean;
};

export async function getAdminUsers(): Promise<AdminUser[]> {
  const response = await http.get<AdminUser[]>("/admin/users");
  return response.data;
}

export async function createAdminUser(payload: CreateAdminUserPayload): Promise<AdminUser> {
  const response = await http.post<AdminUser>("/admin/users", payload);
  return response.data;
}

export async function updateAdminUser(id: string, payload: UpdateAdminUserPayload): Promise<AdminUser> {
  const response = await http.put<AdminUser>(`/admin/users/${id}`, payload);
  return response.data;
}

export async function deleteAdminUser(id: string): Promise<void> {
  await http.delete(`/admin/users/${id}`);
}

export async function getAdminRoles(): Promise<AdminRole[]> {
  const response = await http.get<AdminRole[]>("/admin/roles");
  return response.data;
}

export async function createAdminRole(payload: UpsertRolePayload): Promise<AdminRole> {
  const response = await http.post<AdminRole>("/admin/roles", payload);
  return response.data;
}

export async function updateAdminRole(id: string, payload: UpsertRolePayload): Promise<AdminRole> {
  const response = await http.put<AdminRole>(`/admin/roles/${id}`, payload);
  return response.data;
}

export async function deleteAdminRole(id: string): Promise<void> {
  await http.delete(`/admin/roles/${id}`);
}

export async function getAdminPipelineStages(): Promise<AdminPipelineStage[]> {
  const response = await http.get<AdminPipelineStage[]>("/admin/pipeline-stages");
  return response.data;
}

export async function createAdminPipelineStage(payload: UpsertStagePayload): Promise<AdminPipelineStage> {
  const response = await http.post<AdminPipelineStage>("/admin/pipeline-stages", payload);
  return response.data;
}

export async function updateAdminPipelineStage(id: string, payload: UpsertStagePayload): Promise<AdminPipelineStage> {
  const response = await http.put<AdminPipelineStage>(`/admin/pipeline-stages/${id}`, payload);
  return response.data;
}

export async function deleteAdminPipelineStage(id: string): Promise<void> {
  await http.delete(`/admin/pipeline-stages/${id}`);
}

export async function getAdminPermissions(): Promise<string[]> {
  const response = await http.get<string[]>("/admin/permissions");
  return response.data;
}
