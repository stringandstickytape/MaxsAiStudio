import * as React from "react"
import * as CheckboxPrimitive from "@radix-ui/react-checkbox"
import { Check } from "lucide-react"

import { cn } from "@/lib/utils"

// Define themeable properties for the Checkbox component
export const themeableProps = {};

const Checkbox = React.forwardRef<
  React.ElementRef<typeof CheckboxPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof CheckboxPrimitive.Root>
>(({ className, ...props }, ref) => (
  <CheckboxPrimitive.Root
    ref={ref}
    className={cn(
      "peer h-4 w-4 shrink-0 cursor-pointer transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
      className
    )}
    style={{
      backgroundColor: props.checked 
        ? 'var(--global-primary-color)' 
        : 'var(--global-background-color)',
      borderColor: props.checked 
        ? 'var(--global-primary-color)' 
        : 'var(--global-secondary-color)',
      borderWidth: '1px',
      borderStyle: 'solid',
      borderRadius: 'var(--global-border-radius)',
      boxShadow: 'var(--global-box-shadow)',
    }}
    {...props}
  >
    <CheckboxPrimitive.Indicator
      className={cn("flex items-center justify-center")}
      style={{
        color: props.checked 
          ? 'var(--global-background-color)' 
          : 'transparent',
      }}
    >
      <Check className="h-4 w-4" />
    </CheckboxPrimitive.Indicator>
  </CheckboxPrimitive.Root>
))
Checkbox.displayName = CheckboxPrimitive.Root.displayName

export { Checkbox }


