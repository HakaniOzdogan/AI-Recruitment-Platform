import { PropsWithChildren } from "react";

export function FormActions({ children }: PropsWithChildren): JSX.Element {
  return <div className="row-actions">{children}</div>;
}
