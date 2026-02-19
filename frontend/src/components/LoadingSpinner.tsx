export function LoadingSpinner({ label = "Yükleniyor..." }: { label?: string }): JSX.Element {
  return <span className="loading-spinner">{label}</span>;
}
