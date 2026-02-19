import { ReactNode } from "react";

type Props = {
  role: "system" | "candidate" | "hiring";
  children: ReactNode;
};

export function ChatBubble({ role, children }: Props): JSX.Element {
  const own = role === "candidate" || role === "hiring";

  return (
    <div className={`chat-bubble ${own ? "own" : "system"}`}>
      <small>{role}</small>
      <p>{children}</p>
    </div>
  );
}
