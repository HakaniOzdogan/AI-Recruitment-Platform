import * as React from "react";
import { cn } from "../../lib/utils";

export function Separator({ className, ...props }: React.HTMLAttributes<HTMLHRElement>): JSX.Element {
  return <hr className={cn("separator", className)} {...props} />;
}
