import { ArrowRight, Inbox } from "lucide-react";

export function EmptyState({
  title,
  message,
  actionLabel,
  onAction
}: {
  title: string;
  message: string;
  actionLabel?: string;
  onAction?: () => void;
}): JSX.Element {
  return (
    <div className="empty-state" role="status" aria-live="polite">
      <div className="state-icon" aria-hidden>
        <Inbox size={18} />
      </div>
      <strong>{title}</strong>
      <p className="muted">{message}</p>
      {actionLabel && onAction ? (
        <button type="button" className="button secondary" onClick={onAction}>
          {actionLabel}
          <ArrowRight size={14} />
        </button>
      ) : null}
    </div>
  );
}
