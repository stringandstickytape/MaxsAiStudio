// AiStudioClient\src\components\ConvView\ScrollToBottomButton.tsx
import { useStickToBottomContext } from 'use-stick-to-bottom';
import { ArrowDown } from 'lucide-react';
import React, { useCallback, useEffect, useState } from 'react';

interface ScrollToBottomButtonProps {
  onActivateSticking: () => void;
  stickToBottomEnabled?: boolean;
  scrollContainerRef?: React.RefObject<HTMLDivElement>;
  chatSpaceWidth?: string;
}

// Custom comparison function for ScrollToBottomButton memoization
const areScrollButtonPropsEqual = (prevProps: ScrollToBottomButtonProps, nextProps: ScrollToBottomButtonProps) => {
  return (
    prevProps.stickToBottomEnabled === nextProps.stickToBottomEnabled &&
    prevProps.scrollContainerRef === nextProps.scrollContainerRef &&
    prevProps.chatSpaceWidth === nextProps.chatSpaceWidth
  );
};

export const ScrollToBottomButton = React.memo(({ 
  onActivateSticking, 
  stickToBottomEnabled = true, 
  scrollContainerRef,
  chatSpaceWidth = 'full'
}: ScrollToBottomButtonProps) => {
  
  // State for manual scroll position tracking when stick-to-bottom is disabled
  const [isAtBottomManual, setIsAtBottomManual] = useState(true);
  
  // Try to get context, but handle the case where it doesn't exist
  let stickToBottomContext = null;
  try {
    if (stickToBottomEnabled) {
      stickToBottomContext = useStickToBottomContext();
    }
  } catch (error) {
    // Context not available, we'll use manual tracking
  }

  // Manual scroll position tracking for when stick-to-bottom is disabled
  const checkIfAtBottom = useCallback(() => {
    if (!scrollContainerRef?.current) return;
    
    const element = scrollContainerRef.current;
    const isAtBottom = Math.abs(element.scrollHeight - element.clientHeight - element.scrollTop) < 5;
    setIsAtBottomManual(isAtBottom);
  }, [scrollContainerRef]);

  // Set up scroll listener when stick-to-bottom is disabled
  useEffect(() => {
    if (stickToBottomEnabled || !scrollContainerRef?.current) return;

    const element = scrollContainerRef.current;
    element.addEventListener('scroll', checkIfAtBottom, { passive: true });
    
    // Initial check
    checkIfAtBottom();
    
    return () => {
      element.removeEventListener('scroll', checkIfAtBottom);
    };
  }, [stickToBottomEnabled, scrollContainerRef, checkIfAtBottom]);

  const handleScrollToBottom = () => {
    // Enable sticking to bottom when button is clicked
    onActivateSticking();
    
    if (stickToBottomContext) {
      // Use library's scroll method
      stickToBottomContext.scrollToBottom();
    } else if (scrollContainerRef?.current) {
      // Manual scroll to bottom
      const element = scrollContainerRef.current;
      element.scrollTo({
        top: element.scrollHeight,
        behavior: 'smooth'
      });
    }
  };

  // Determine if we're at bottom based on available context
  const isAtBottom = stickToBottomEnabled 
    ? (stickToBottomContext?.isAtBottom ?? true)
    : isAtBottomManual;

  // Only show the button when not at bottom
  if (isAtBottom) return null;

  return (
    <button
      className="absolute left-1/2 -translate-x-1/2 bottom-4 z-10 rounded-full p-2 shadow-md transition-colors ScrollToBottomButton"
      onClick={handleScrollToBottom}
      aria-label="Scroll to bottom"
      style={{
        backgroundColor: 'var(--global-primary-color)',
        color: 'var(--global-background-color)',
        borderRadius: 'var(--global-border-radius)',
        boxShadow: 'var(--global-box-shadow)'
      }}
    >
      <ArrowDown className="h-5 w-5" />
    </button>
  );
}, areScrollButtonPropsEqual);