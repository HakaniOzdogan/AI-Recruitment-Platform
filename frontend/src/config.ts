export type AppRuntimeConfig = {
  API_BASE_URL?: string;
};

function fromRuntime(): string | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }

  return window.__APP_CONFIG__?.API_BASE_URL?.trim();
}

const runtimeApiBaseUrl = fromRuntime();
const viteApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim();
const viteAuthEnforced = import.meta.env.VITE_AUTH_ENFORCED?.trim().toLowerCase();

function resolveAuthEnforced(): boolean {
  if (viteAuthEnforced === "true") return true;
  if (viteAuthEnforced === "false") return false;
  return !import.meta.env.DEV;
}

export const API_BASE_URL = import.meta.env.DEV
  ? viteApiBaseUrl || runtimeApiBaseUrl || "http://localhost:8080"
  : runtimeApiBaseUrl || viteApiBaseUrl || "http://localhost:8080";

export const AUTH_ENFORCED = resolveAuthEnforced();
