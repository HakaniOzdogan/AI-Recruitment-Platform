export function LoadingSkeleton({ lines = 3 }: { lines?: number }): JSX.Element {
  return (
    <div className="skeleton-block" aria-hidden>
      {Array.from({ length: lines }).map((_, idx) => (
        <span key={idx} className="skeleton-line" />
      ))}
    </div>
  );
}
