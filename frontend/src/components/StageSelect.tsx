type Props = {
  value: string;
  options: Array<{ id: string; name: string }>;
  onChange: (next: string) => void;
  disabled?: boolean;
};

export function StageSelect({ value, options, onChange, disabled = false }: Props): JSX.Element {
  return (
    <div className="row-actions">
      <select value={value} onChange={(e) => onChange(e.target.value)} disabled={disabled}>
        <option value="">Stage seç</option>
        {options.map((opt) => (
          <option key={opt.id} value={opt.id}>
            {opt.name}
          </option>
        ))}
      </select>
      <input
        placeholder="veya stageId"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
      />
    </div>
  );
}
