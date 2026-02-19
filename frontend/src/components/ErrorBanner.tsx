import { useSyncExternalStore } from "react";
import { clearForbidden, getSnapshot, subscribe } from "../store/uiFeedback";

export function ErrorBanner(): JSX.Element | null {
  const state = useSyncExternalStore(subscribe, getSnapshot);
  if (!state.forbiddenMessage) {
    return null;
  }

  return (
    <div className="error-banner">
      <span>{state.forbiddenMessage}</span>
      <button type="button" onClick={() => clearForbidden()}>
        Kapat
      </button>
    </div>
  );
}
