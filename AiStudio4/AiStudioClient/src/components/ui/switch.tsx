// AiStudioClient/src/components/ui/switch.tsx
import * as React from "react"
import * as SwitchPrimitives from "@radix-ui/react-switch"

import { cn } from "@/lib/utils"

// Define themeable properties for the Switch component
export const themeableProps = {};

const Switch = React.forwardRef<
  React.ElementRef<typeof SwitchPrimitives.Root>,
  React.ComponentPropsWithoutRef<typeof SwitchPrimitives.Root>
>(({ className, ...props }, ref) => (
  <SwitchPrimitives.Root
    className={cn(
      "Switch relative inline-flex h-6 w-14 shrink-0 cursor-pointer items-center justify-between px-1 rounded-full border transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50",
      className
    )}
    style={{
      backgroundColor: props.checked 
        ? 'var(--global-primary-color)' 
        : 'var(--global-background-color)',
      borderColor: props.checked 
        ? 'var(--global-primary-color)' 
        : 'var(--global-border-color)',
      boxShadow: 'var(--global-box-shadow)',
    }}
    {...props}
    ref={ref}
  >
    <SwitchPrimitives.Thumb
      className={cn(
        "pointer-events-none absolute block h-5 w-5 rounded-full shadow-md ring-0 transition-transform",
        "data-[state=checked]:translate-x-8 data-[state=unchecked]:translate-x-0"
      )}
      style={{
        backgroundColor: props.checked 
          ? 'var(--global-secondary-color)' 
          : 'var(--global-text-color)',
        border: `1px solid ${props.checked 
          ? 'var(--global-primary-color)' 
          : 'var(--global-border-color)'}`,
        left: '2px',
        top: '2px'
      }}
    />
  </SwitchPrimitives.Root>
))
Switch.displayName = SwitchPrimitives.Root.displayName

export { Switch }