export function FieldError({ message }: { message?: string | null }): JSX.Element | null {
  if (!message) {
    return null;
  }

  return <p className="inline-error">{message}</p>;
}
