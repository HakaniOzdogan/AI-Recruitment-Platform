import { AxiosError } from "axios";

type ValidationLine = {
  field: string;
  message: string;
};

function extractLines(payload: unknown): ValidationLine[] {
  if (!payload || typeof payload !== "object") {
    return [];
  }

  const data = payload as Record<string, unknown>;
  const lines: ValidationLine[] = [];

  if (Array.isArray(data.errors)) {
    for (const item of data.errors) {
      if (item && typeof item === "object") {
        const row = item as Record<string, unknown>;
        const field = typeof row.field === "string" ? row.field : "request";
        const message = typeof row.message === "string" ? row.message : "Invalid value";
        lines.push({ field, message });
      }
    }
  }

  const modelErrors = data.errors;
  if (modelErrors && typeof modelErrors === "object" && !Array.isArray(modelErrors)) {
    for (const [field, raw] of Object.entries(modelErrors as Record<string, unknown>)) {
      if (Array.isArray(raw)) {
        for (const entry of raw) {
          if (typeof entry === "string") {
            lines.push({ field, message: entry });
          }
        }
      }
    }
  }

  return lines;
}

function extractCorrelationId(error: AxiosError): string | null {
  const enriched = error as AxiosError & { correlationId?: string };
  if (enriched.correlationId) {
    return enriched.correlationId;
  }

  const responseId = error.response?.headers?.["x-correlation-id"];
  if (typeof responseId === "string") {
    return responseId;
  }

  const reqHeaders = error.config?.headers as Record<string, unknown> | undefined;
  const reqId = reqHeaders?.["X-Correlation-Id"] ?? reqHeaders?.["x-correlation-id"];
  return typeof reqId === "string" ? reqId : null;
}

export function JsonErrorBox({ error }: { error: unknown }): JSX.Element | null {
  let payload: unknown = null;
  let correlationId: string | null = null;

  if (error && typeof error === "object" && "isAxiosError" in error) {
    payload = (error as AxiosError).response?.data;
    correlationId = extractCorrelationId(error as AxiosError);
  }

  const lines = extractLines(payload);
  if (lines.length === 0 && !correlationId) {
    return null;
  }

  return (
    <div className="json-error-box">
      {lines.length > 0 ? (
        <>
          <strong>Validation errors:</strong>
          <ul>
            {lines.map((line, index) => (
              <li key={`${line.field}-${index}`}>
                <code>{line.field}</code>: {line.message}
              </li>
            ))}
          </ul>
        </>
      ) : null}
      {correlationId ? <p className="muted">Ref: {correlationId}</p> : null}
    </div>
  );
}
