export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: JSX.Element }): JSX.Element {
  return (
    <header className="page-header">
      <div>
        <h2>{title}</h2>
        {subtitle ? <p className="muted page-subtitle">{subtitle}</p> : null}
      </div>
      {actions ? <div className="row-actions">{actions}</div> : null}
    </header>
  );
}
