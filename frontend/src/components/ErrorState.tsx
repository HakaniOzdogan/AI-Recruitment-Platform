import { AlertTriangle, RotateCcw } from "lucide-react";

export function ErrorState({
  message,
  onRetry,
  reference
}: {
  message: string;
  onRetry?: () => void;
  reference?: string | null;
}): JSX.Element {
  return (
    <div className="error-state" role="alert">
      <div className="state-icon" aria-hidden>
        <AlertTriangle size={18} />
      </div>
      <p>{message}</p>
      {reference ? <p className="muted">Ref: {reference}</p> : null}
      {onRetry ? (
        <button type="button" className="button secondary" onClick={onRetry}>
          <RotateCcw size={14} />
          Retry
        </button>
      ) : null}
    </div>
  );
}
