import axios, { AxiosError } from "axios";
import { API_BASE_URL, AUTH_ENFORCED } from "../config";
import { getToken, logout, triggerUnauthorized } from "../auth/auth";
import { pushToast, setForbidden } from "../store/uiFeedback";

type RequestMeta = {
  correlationId?: string;
  suppressErrorStatuses?: number[];
};

function getServerMessage(payload: unknown): string {
  if (!payload || typeof payload !== "object") {
    return "İstek başarısız";
  }

  const data = payload as Record<string, unknown>;

  if (typeof data.message === "string" && data.message.length > 0) {
    return data.message;
  }

  if (typeof data.title === "string" && data.title.length > 0) {
    return data.title;
  }

  if (Array.isArray(data.errors) && data.errors.length > 0) {
    const first = data.errors[0];
    if (first && typeof first === "object" && typeof (first as Record<string, unknown>).message === "string") {
      return (first as Record<string, unknown>).message as string;
    }
  }

  return "İstek başarısız";
}

function getHeaderValue(headers: unknown, key: string): string | null {
  if (!headers || typeof headers !== "object") {
    return null;
  }

  const normalizedKey = key.toLowerCase();
  const h = headers as Record<string, unknown> & { get?: (name: string) => string | null };
  if (typeof h.get === "function") {
    const viaGet = h.get(normalizedKey) ?? h.get(key);
    if (viaGet) {
      return viaGet;
    }
  }

  const direct = h[normalizedKey] ?? h[key];
  return typeof direct === "string" ? direct : null;
}

function resolveCorrelationId(error: AxiosError): string | null {
  const responseId = getHeaderValue(error.response?.headers, "x-correlation-id");
  if (responseId) {
    return responseId;
  }

  const configWithMeta = error.config as (typeof error.config & { meta?: { correlationId?: string } }) | undefined;
  const metaId = configWithMeta?.meta?.correlationId;
  if (metaId) {
    return metaId;
  }

  return getHeaderValue(error.config?.headers, "x-correlation-id");
}

function shouldSuppressToast(error: AxiosError, status: number | undefined): boolean {
  if (!status) {
    return false;
  }

  const configWithMeta = error.config as (typeof error.config & { meta?: RequestMeta }) | undefined;
  const suppressed = configWithMeta?.meta?.suppressErrorStatuses;
  return Array.isArray(suppressed) && suppressed.includes(status);
}

export const http = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000
});

http.interceptors.request.use((config) => {
  config.headers = (config.headers ?? {}) as typeof config.headers;
  const correlationId = crypto.randomUUID();
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  config.headers["X-Correlation-Id"] = correlationId;
  (config as typeof config & { meta?: { correlationId?: string } }).meta = {
    ...(config as typeof config & { meta?: { correlationId?: string } }).meta,
    correlationId
  };

  if (import.meta.env.DEV) {
    const method = (config.method ?? "get").toUpperCase();
    const url = `${config.baseURL ?? ""}${config.url ?? ""}`;
    console.debug("[api]", method, url);
  }
  return config;
});

http.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;
    const data = error.response?.data;
    const correlationId = resolveCorrelationId(error);
    (error as AxiosError & { correlationId?: string }).correlationId = correlationId ?? undefined;
    const correlationRef = correlationId ? ` Ref: ${correlationId}` : "";
    const suppressToast = shouldSuppressToast(error, status);

    if (status === 401) {
      if (AUTH_ENFORCED) {
        logout();
        triggerUnauthorized();
      }
      return Promise.reject(error);
    }

    if (status === 403) {
      setForbidden("Yetkin yok");
      if (!suppressToast) {
        pushToast("error", `Yetkin yok${correlationRef}`);
      }
      return Promise.reject(error);
    }

    if (status === 400 || status === 409) {
      if (!suppressToast) {
        pushToast("error", `${getServerMessage(data)}${correlationRef}`);
      }
      return Promise.reject(error);
    }

    if (status && status >= 500) {
      if (!suppressToast) {
        pushToast("error", `Sunucu hatası${correlationRef}`);
      }
      return Promise.reject(error);
    }

    if (!suppressToast) {
      pushToast("error", `${getServerMessage(data)}${correlationRef}`);
    }
    return Promise.reject(error);
  }
);

export type ApiListResponse<T> = T[];
