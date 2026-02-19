import { getTokenRoles } from "./auth";

function hasAnyRole(expected: string[]): boolean {
  const roles = getTokenRoles().map((x) => x.toLowerCase());
  return expected.some((x) => roles.includes(x.toLowerCase()));
}

export function canAccessAdminPanel(): boolean {
  return hasAnyRole(["Admin"]);
}

export function canManageJobs(): boolean {
  return hasAnyRole(["Admin", "HR"]);
}

export function canManageCandidates(): boolean {
  return hasAnyRole(["Admin", "HR"]);
}

export function canManageApplications(): boolean {
  return hasAnyRole(["Admin", "HR", "HiringManager"]);
}

export function canApplyAndInterview(): boolean {
  return hasAnyRole(["Admin", "HR", "HiringManager", "User"]);
}

export function canViewReports(): boolean {
  return hasAnyRole(["Admin", "HR", "HiringManager"]);
}
