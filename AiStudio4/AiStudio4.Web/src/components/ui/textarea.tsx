import * as React from "react";

import { cn } from "@/lib/utils";

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  showLineCount?: boolean;
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, showLineCount = true, ...props }, ref) => {
    const [lineCount, setLineCount] = React.useState(1);
    const textareaRef = React.useRef<HTMLTextAreaElement>(null);
    
    // Combine refs
    const handleRefs = (el: HTMLTextAreaElement | null) => {
      textareaRef.current = el;
      if (typeof ref === 'function') {
        ref(el);
      } else if (ref) {
        ref.current = el;
      }
    };
    
    // Calculate line count efficiently using a debounced approach
    const updateLineCount = React.useCallback(() => {
      if (textareaRef.current) {
        const text = textareaRef.current.value;
        const lines = text ? text.split('\n').length : 1;
        setLineCount(lines);
      }
    }, []);
    
    // Use a debounced event handler to avoid performance issues
    const debouncedUpdate = React.useMemo(() => {
      let timeoutId: NodeJS.Timeout;
      return () => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(updateLineCount, 100);
      };
    }, [updateLineCount]);
    
    React.useEffect(() => {
      if (showLineCount && textareaRef.current) {
        // Set initial line count
        updateLineCount();
        
        // Add event listeners
        const textarea = textareaRef.current;
        textarea.addEventListener('input', debouncedUpdate);
        textarea.addEventListener('keydown', debouncedUpdate);
        textarea.addEventListener('paste', debouncedUpdate);
        
        return () => {
          textarea.removeEventListener('input', debouncedUpdate);
          textarea.removeEventListener('keydown', debouncedUpdate);
          textarea.removeEventListener('paste', debouncedUpdate);
        };
      }
    }, [showLineCount, debouncedUpdate, updateLineCount]);
    return (
      <div className="relative h-full">
        {showLineCount && (
          <div className="absolute right-2 bottom-1 text-xs text-gray-100 pointer-events-none select-none opacity-90">
            {lineCount} {lineCount === 1 ? 'line' : 'lines'}
          </div>
        )}
        <textarea
          className={cn(
            "flex min-h-[80px] w-full h-full rounded-md border border-gray-700 bg-gray-800 px-3 py-2 text-sm ring-offset-background placeholder:text-gray-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
            className
          )}
          ref={handleRefs}
          {...props}
        />
      </div>
    );
  }
);
Textarea.displayName = "Textarea";

export { Textarea };