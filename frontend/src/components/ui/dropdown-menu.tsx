import * as React from "react";
import { cn } from "../../lib/utils";

export function DropdownMenu({ children }: { children: React.ReactNode }): JSX.Element {
  return <div className="dropdown-root">{children}</div>;
}

export function DropdownMenuTrigger({
  className,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement>): JSX.Element {
  return <button type="button" className={cn("button ghost", className)} {...props} />;
}

export function DropdownMenuContent({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
  return <div className={cn("dropdown-content", className)} {...props} />;
}

export function DropdownMenuItem({
  className,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement>): JSX.Element {
  return <button type="button" className={cn("dropdown-item", className)} {...props} />;
}
