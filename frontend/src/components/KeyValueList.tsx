type Entry = {
  key: string;
  value: string;
};

export function KeyValueList({ title, items }: { title?: string; items: Entry[] }): JSX.Element {
  if (items.length === 0) {
    return <p className="muted">Veri yok</p>;
  }

  return (
    <div>
      {title ? <h4>{title}</h4> : null}
      <ul className="kv-list">
        {items.map((item) => (
          <li key={`${item.key}-${item.value}`}>
            <strong>{item.key}:</strong> {item.value}
          </li>
        ))}
      </ul>
    </div>
  );
}
