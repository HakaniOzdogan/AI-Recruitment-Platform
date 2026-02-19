export function TableSkeleton({ rows = 5 }: { rows?: number }): JSX.Element {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Loading</th>
            <th>Loading</th>
            <th>Loading</th>
            <th>Loading</th>
          </tr>
        </thead>
        <tbody>
          {Array.from({ length: rows }).map((_, idx) => (
            <tr key={idx}>
              <td colSpan={4}>
                <div className="skeleton-line" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
