import * as React from "react";
import TextareaAutosize from 'react-textarea-autosize';

import { cn } from "@/lib/utils";

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  showLineCount?: boolean;
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, showLineCount = true, ...props }, ref) => {
    const [lineCount, setLineCount] = React.useState(1);
    const textareaRef = React.useRef<HTMLTextAreaElement>(null);
    
    
    const handleRefs = (el: HTMLTextAreaElement | null) => {
      textareaRef.current = el;
      if (typeof ref === 'function') {
        ref(el);
      } else if (ref) {
        ref.current = el;
      }
    };
    
    
    const updateLineCount = React.useCallback(() => {
      if (textareaRef.current) {
        const text = textareaRef.current.value;
        const lines = text ? text.split('\n').length : 1;
        setLineCount(lines);
      }
    }, []);
    
    
    const debouncedUpdate = React.useMemo(() => {
      let timeoutId: NodeJS.Timeout;
      return () => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(updateLineCount, 100);
      };
    }, [updateLineCount]);
    
    React.useEffect(() => {
      if (showLineCount && textareaRef.current) {
        
        updateLineCount();
        
        
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
      <div className="relative">
        {showLineCount && (
          <div className="absolute right-2 bottom-1 text-xs text-gray-100 pointer-events-none select-none opacity-90">
            {lineCount} {lineCount === 1 ? 'line' : 'lines'}
          </div>
        )}
        <TextareaAutosize
          className={cn(
            "flex w-full rounded-md focus:outline-none focus:ring-0 focus:border-transparent bg-gray-800 px-2 py-1 text-sm placeholder:text-gray-400  disabled:cursor-not-allowed disabled:opacity-50 resize-none",
            className
          )}
          ref={handleRefs}
          minRows={2}
          maxRows={10}
          style={props.style}
          {...props}
        />
      </div>
    );
  }
);
Textarea.displayName = "Textarea";

export { Textarea };