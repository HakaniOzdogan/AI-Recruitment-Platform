export function TagList({ tags }: { tags: string[] }): JSX.Element {
  if (tags.length === 0) {
    return <p className="muted">Skill bulunamadı</p>;
  }

  return (
    <div className="tag-list">
      {tags.map((tag) => (
        <span key={tag} className="tag-item">
          {tag}
        </span>
      ))}
    </div>
  );
}
