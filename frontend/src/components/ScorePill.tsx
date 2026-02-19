export function ScorePill({ score }: { score: number }): JSX.Element {
  const tone = score >= 70 ? "good" : score >= 40 ? "warn" : "bad";
  return <span className={`score-pill ${tone}`}>{score}</span>;
}
