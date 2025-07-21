// MarkdownPane/CodeBlockScrollButton.tsx
import { ArrowDown, ArrowUp } from 'lucide-react';
import React, { useState, useEffect, useRef } from 'react';

interface CodeBlockScrollButtonProps {
  stickToBottomInstance: any; // StickToBottom instance
}

export const CodeBlockScrollButton = React.memo(({ stickToBottomInstance }: CodeBlockScrollButtonProps) => {
  const [isHoveredBottom, setIsHoveredBottom] = useState(false);
  const [isHoveredTop, setIsHoveredTop] = useState(false);
  const [isAtTop, setIsAtTop] = useState(true);
  
  // Check if at top of scroll container
  useEffect(() => {
    const scrollRef = stickToBottomInstance?.scrollRef;
    if (!scrollRef?.current) return;

    const checkIfAtTop = () => {
      const element = scrollRef.current;
      setIsAtTop(element.scrollTop <= 5); // Small threshold for "at top"
    };

    const element = scrollRef.current;
    element.addEventListener('scroll', checkIfAtTop, { passive: true });
    checkIfAtTop(); // Initial check
    
    return () => {
      element.removeEventListener('scroll', checkIfAtTop);
    };
  }, [stickToBottomInstance?.scrollRef]);
  
  const handleScrollToBottom = () => {
    if (stickToBottomInstance?.scrollToBottom) {
      stickToBottomInstance.scrollToBottom();
    }
  };

  const handleScrollToTop = () => {
    const scrollRef = stickToBottomInstance?.scrollRef;
    if (scrollRef?.current) {
      scrollRef.current.scrollTo({
        top: 0,
        behavior: 'smooth'
      });
    }
  };

  // Only show buttons when not at respective positions
  const isAtBottom = stickToBottomInstance?.isAtBottom ?? true;
  const showBottomButton = !isAtBottom;
  const showTopButton = !isAtTop;
  
  // Don't render anything if both buttons are hidden
  if (!showBottomButton && !showTopButton) return null;

  const buttonStyle = {
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
  };

  return (
    <div className="absolute right-2 bottom-2 z-10 flex flex-col gap-1">
      {/* Scroll to top button */}
      {showTopButton && (
        <button
          className="rounded-full p-1.5 shadow-md transition-all hover:shadow-lg"
          onClick={handleScrollToTop}
          onMouseEnter={() => setIsHoveredTop(true)}
          onMouseLeave={() => setIsHoveredTop(false)}
          aria-label="Scroll to top of code block"
          style={{
            ...buttonStyle,
            backgroundColor: isHoveredTop ? 'var(--global-background-color)' : 'rgba(0, 0, 0, 0.7)',
          }}
        >
          <ArrowUp className="h-3.5 w-3.5" />
        </button>
      )}
      
      {/* Scroll to bottom button */}
      {showBottomButton && (
        <button
          className="rounded-full p-1.5 shadow-md transition-all hover:shadow-lg"
          onClick={handleScrollToBottom}
          onMouseEnter={() => setIsHoveredBottom(true)}
          onMouseLeave={() => setIsHoveredBottom(false)}
          aria-label="Scroll to bottom of code block"
          style={{
            ...buttonStyle,
            backgroundColor: isHoveredBottom ? 'var(--global-background-color)' : 'rgba(0, 0, 0, 0.7)',
          }}
        >
          <ArrowDown className="h-3.5 w-3.5" />
        </button>
      )}
    </div>
  );
});