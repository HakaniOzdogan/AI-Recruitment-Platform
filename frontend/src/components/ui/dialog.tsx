import * as React from "react";
import { cn } from "../../lib/utils";

export function Dialog({ open, children }: { open: boolean; children: React.ReactNode }): JSX.Element | null {
  if (!open) {
    return null;
  }

  return <div className="modal-overlay">{children}</div>;
}

export function DialogContent({ className, ...props }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
  return <div className={cn("modal-card", className)} {...props} />;
}

export function DialogHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
  return <div className={cn("row-actions", className)} {...props} />;
}

export function DialogTitle({ className, ...props }: React.HTMLAttributes<HTMLHeadingElement>): JSX.Element {
  return <h3 className={cn(className)} {...props} />;
}
