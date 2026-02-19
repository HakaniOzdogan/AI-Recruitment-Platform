type Mode = "OFF" | "ASSIST" | "ADAPTIVE";

export function ModeToggle({ value, onChange, disabled = false }: { value: Mode; onChange: (next: Mode) => void; disabled?: boolean }): JSX.Element {
  return (
    <div className="row-actions">
      {(["OFF", "ASSIST", "ADAPTIVE"] as Mode[]).map((mode) => (
        <button key={mode} type="button" disabled={disabled} className={value === mode ? "mode-active" : ""} onClick={() => onChange(mode)}>
          {mode}
        </button>
      ))}
    </div>
  );
}
