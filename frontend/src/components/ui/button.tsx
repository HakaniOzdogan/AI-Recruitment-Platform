import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "../../lib/utils";

const buttonVariants = cva("button", {
  variants: {
    variant: {
      default: "",
      secondary: "secondary",
      ghost: "ghost"
    }
  },
  defaultVariants: {
    variant: "default"
  }
});

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonVariants> {}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(({ className, variant, ...props }, ref) => {
  return <button ref={ref} className={cn(buttonVariants({ variant }), className)} {...props} />;
});

Button.displayName = "Button";
