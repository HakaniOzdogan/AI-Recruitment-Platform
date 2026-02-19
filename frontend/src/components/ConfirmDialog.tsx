import { PropsWithChildren } from "react";

type Props = PropsWithChildren<{
  title: string;
  open: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  confirmText?: string;
  showActions?: boolean;
}>;

export function ConfirmDialog({
  title,
  open,
  onConfirm,
  onCancel,
  confirmText = "Onayla",
  showActions = true,
  children
}: Props): JSX.Element | null {
  if (!open) {
    return null;
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card">
        <h3>{title}</h3>
        <div>{children}</div>
        {showActions ? (
          <div className="row-actions">
            <button type="button" onClick={onCancel}>
              Vazgeç
            </button>
            <button type="button" onClick={onConfirm}>
              {confirmText}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
