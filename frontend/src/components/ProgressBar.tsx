type Props = {
  value: number;
};

export function ProgressBar({ value }: Props): JSX.Element {
  const safeValue = Math.max(0, Math.min(100, Math.round(value)));
  return (
    <div style={{ width: "100%" }}>
      <progress max={100} value={safeValue} style={{ width: "100%" }} />
      <p className="muted">{safeValue}%</p>
    </div>
  );
}
