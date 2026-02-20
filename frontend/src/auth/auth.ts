import { loginRequest, registerRequest } from "../api/endpoints/auth";

const TOKEN_KEY = "ik_demo_access_token";

let unauthorizedHandler: (() => void) | null = null;

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

export function triggerUnauthorized(): void {
  if (unauthorizedHandler) {
    unauthorizedHandler();
  }
}

export function logout(): void {
  localStorage.removeItem(TOKEN_KEY);
}

export async function login(email: string, password: string): Promise<void> {
  const result = await loginRequest({ email, password });
  setToken(result.accessToken);
}

export async function register(fullName: string, email: string, password: string): Promise<void> {
  const result = await registerRequest({ fullName, email, password });
  setToken(result.accessToken);
}

export function isAuthenticated(): boolean {
  return Boolean(getToken());
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split(".");
  if (parts.length < 2) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function getTokenRoles(): string[] {
  const token = getToken();
  if (!token) {
    return [];
  }

  const payload = decodeJwtPayload(token);
  if (!payload) {
    return [];
  }

  const roleKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  const raw = payload[roleKey] ?? payload.role;
  if (Array.isArray(raw)) {
    return raw.filter((x): x is string => typeof x === "string");
  }

  if (typeof raw === "string") {
    return [raw];
  }

  return [];
}

export function hasRole(role: string): boolean {
  return getTokenRoles().some((x) => x.toLowerCase() === role.toLowerCase());
}

export type TokenUserInfo = {
  fullName: string;
  roles: string[];
  primaryRole: string;
  initials: string;
};

function normalizeRoleLabel(role: string): string {
  const map: Record<string, string> = {
    hr: "HR",
    recruiter: "Recruiter",
    applicant: "Applicant",
    user: "User",
    admin: "Admin",
    hiringmanager: "HiringManager",
    interviewer: "Interviewer"
  };
  const key = role.replace(/\s+/g, "").toLowerCase();
  return map[key] ?? role;
}

function getTokenName(payload: Record<string, unknown>): string {
  const nameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
  const raw = payload.name ?? payload[nameClaim];
  if (typeof raw !== "string" || raw.trim().length === 0) {
    return "Kullanıcı";
  }
  return raw.trim();
}

function getInitials(fullName: string): string {
  const parts = fullName
    .split(" ")
    .map((x) => x.trim())
    .filter((x) => x.length > 0);
  if (parts.length === 0) {
    return "??";
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ""}${parts[1][0] ?? ""}`.toUpperCase();
}

export function getTokenUserInfo(): TokenUserInfo {
  const token = getToken();
  if (!token) {
    return {
      fullName: "Kullanıcı",
      roles: [],
      primaryRole: "User",
      initials: "US"
    };
  }

  const payload = decodeJwtPayload(token);
  const fullName = payload ? getTokenName(payload) : "Kullanıcı";
  const roles = getTokenRoles().map(normalizeRoleLabel);
  const primaryRole = roles[0] ?? "User";

  return {
    fullName,
    roles,
    primaryRole,
    initials: getInitials(fullName)
  };
}
