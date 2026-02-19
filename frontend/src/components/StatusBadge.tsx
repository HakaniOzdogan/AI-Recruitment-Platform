import { AlertCircle, CheckCircle2, Lock, Sparkles } from "lucide-react";

type BadgeTone = "published" | "draft" | "forbidden" | "insufficient" | "ai" | "neutral";

function toneByLabel(label: string | number | null | undefined): BadgeTone {
  const normalized = String(label ?? "").trim().toLowerCase();
  if (normalized === "published" || normalized === "active") return "published";
  if (normalized === "draft" || normalized === "open") return "draft";
  if (normalized.includes("forbidden") || normalized.includes("yetkin yok")) return "forbidden";
  if (normalized.includes("insufficient") || normalized.includes("kanıt yok") || normalized.includes("kanit yok")) {
    return "insufficient";
  }
  if (normalized.includes("ai") || normalized.includes("adaptive") || normalized.includes("debug")) return "ai";
  return "neutral";
}

export function StatusBadge({ label, tone }: { label: string | number | null | undefined; tone?: BadgeTone }): JSX.Element {
  const resolved = tone ?? toneByLabel(label);
  const safeLabel = String(label ?? "-");

  return (
    <span className={`status-badge status-${resolved}`}>
      {resolved === "published" ? <CheckCircle2 size={12} /> : null}
      {resolved === "forbidden" ? <Lock size={12} /> : null}
      {resolved === "insufficient" ? <AlertCircle size={12} /> : null}
      {resolved === "ai" ? <Sparkles size={12} /> : null}
      {safeLabel}
    </span>
  );
}
