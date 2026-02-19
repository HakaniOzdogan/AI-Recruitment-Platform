import { PropsWithChildren, useState } from "react";

export function Collapsible({ title, children }: PropsWithChildren<{ title: string }>): JSX.Element {
  const [open, setOpen] = useState(false);

  return (
    <div className="collapsible">
      <button type="button" className="collapsible-toggle" onClick={() => setOpen((v) => !v)}>
        {open ? "−" : "+"} {title}
      </button>
      {open ? <div className="collapsible-content">{children}</div> : null}
    </div>
  );
}
