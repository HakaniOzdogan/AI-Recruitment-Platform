import { Candidate } from "../api/endpoints/candidates";

type Props = {
  candidates: Candidate[];
  value: string;
  onChange: (id: string) => void;
  disabled?: boolean;
};

export function CandidateSelect({ candidates, value, onChange, disabled = false }: Props): JSX.Element {
  return (
    <select value={value} onChange={(e) => onChange(e.target.value)} disabled={disabled}>
      <option value="">Aday seç</option>
      {candidates.map((candidate) => (
        <option key={candidate.id} value={candidate.id}>
          {candidate.fullName} ({candidate.email ?? "-"})
        </option>
      ))}
    </select>
  );
}
