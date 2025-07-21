// MarkdownPane/CodeBlockScrollButton.tsx
import { ArrowDown } from 'lucide-react';
import React, { useState } from 'react';

interface CodeBlockScrollButtonProps {
  stickToBottomInstance: any; // StickToBottom instance
}

export const CodeBlockScrollButton = React.memo(({ stickToBottomInstance }: CodeBlockScrollButtonProps) => {
  const [isHovered, setIsHovered] = useState(false);
  
  const handleScrollToBottom = () => {
    if (stickToBottomInstance?.scrollToBottom) {
      stickToBottomInstance.scrollToBottom();
    }
  };

  // Only show the button when not at bottom
  const isAtBottom = stickToBottomInstance?.isAtBottom ?? true;
  if (isAtBottom) return null;

  return (
    <button
      className="absolute right-2 bottom-2 z-10 rounded-full p-1.5 shadow-md transition-all hover:shadow-lg"
      onClick={handleScrollToBottom}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      aria-label="Scroll to bottom of code block"
      style={{
        backgroundColor: isHovered ? 'var(--global-background-color)' : 'rgba(0, 0, 0, 0.7)',
        color: 'var(--global-primary-color)',
        border: '1px solid var(--global-primary-color)',
        borderRadius: '50%',
        width: '28px',
        height: '28px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        boxShadow: '0 2px 8px rgba(0, 0, 0, 0.3)',
        transition: 'background-color 0.2s ease',
        backdropFilter: 'blur(4px)'
      }}
    >
      <ArrowDown className="h-3.5 w-3.5" />
    </button>
  );
});