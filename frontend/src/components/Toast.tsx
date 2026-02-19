import { useEffect, useSyncExternalStore } from "react";
import { getSnapshot, removeToast, subscribe } from "../store/uiFeedback";

export function ToastHost(): JSX.Element {
  const state = useSyncExternalStore(subscribe, getSnapshot);

  useEffect(() => {
    if (state.toasts.length === 0) {
      return;
    }

    const timer = setTimeout(() => {
      removeToast(state.toasts[0].id);
    }, 3500);

    return () => clearTimeout(timer);
  }, [state.toasts]);

  return (
    <div className="toast-host">
      {state.toasts.map((toast) => (
        <div key={toast.id} className={`toast toast-${toast.type}`}>
          <span>{toast.message}</span>
          <button type="button" aria-label="Dismiss notification" onClick={() => removeToast(toast.id)}>
            x
          </button>
        </div>
      ))}
    </div>
  );
}
