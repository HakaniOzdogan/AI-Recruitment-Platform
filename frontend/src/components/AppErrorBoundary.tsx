import React from "react";

type State = {
  hasError: boolean;
  message: string;
};

export class AppErrorBoundary extends React.Component<React.PropsWithChildren, State> {
  constructor(props: React.PropsWithChildren) {
    super(props);
    this.state = { hasError: false, message: "" };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message || "Beklenmeyen bir arayuz hatasi olustu." };
  }

  componentDidCatch(error: Error): void {
    // Keep a console trace for quick local debugging.
    // eslint-disable-next-line no-console
    console.error("UI runtime error:", error);
  }

  render(): React.ReactNode {
    if (this.state.hasError) {
      return (
        <main className="login-wrap">
          <section className="card" style={{ width: "min(560px, 100%)" }}>
            <h2>Sayfa beklenmeyen bir hatayla durdu</h2>
            <p className="muted">Detay: {this.state.message}</p>
            <div className="row-actions">
              <button type="button" onClick={() => window.location.reload()}>
                Sayfayi Yenile
              </button>
            </div>
          </section>
        </main>
      );
    }

    return this.props.children;
  }
}

