import { PropsWithChildren } from "react";

export function PanelCard({ title, children, actions }: PropsWithChildren<{ title: string; actions?: JSX.Element }>): JSX.Element {
  return (
    <section className="card modern-card panel-card section-card">
      <div className="row-actions">
        <h2>{title}</h2>
        {actions ?? null}
      </div>
      {children}
    </section>
  );
}
