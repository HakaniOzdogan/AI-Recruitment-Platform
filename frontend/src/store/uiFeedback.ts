export type ToastItem = {
  id: string;
  type: "error" | "info" | "success";
  message: string;
};

type Listener = () => void;

let toasts: ToastItem[] = [];
let forbiddenMessage: string | null = null;
const listeners = new Set<Listener>();
let snapshot = { toasts, forbiddenMessage };

function refreshSnapshot(): void {
  snapshot = { toasts, forbiddenMessage };
}

function notify(): void {
  listeners.forEach((listener) => listener());
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getSnapshot() {
  return snapshot;
}

export function pushToast(type: ToastItem["type"], message: string): void {
  toasts = [...toasts, { id: crypto.randomUUID(), type, message }];
  refreshSnapshot();
  notify();
}

export function notifySuccess(message: string): void {
  pushToast("success", message);
}

export function notifyError(message: string, reference?: string | null): void {
  pushToast("error", reference ? `${message} Ref: ${reference}` : message);
}

export function removeToast(id: string): void {
  toasts = toasts.filter((item) => item.id !== id);
  refreshSnapshot();
  notify();
}

export function setForbidden(message: string | null): void {
  forbiddenMessage = message;
  refreshSnapshot();
  notify();
}

export function clearForbidden(): void {
  setForbidden(null);
}
