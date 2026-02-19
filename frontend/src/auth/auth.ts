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
