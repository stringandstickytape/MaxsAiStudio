// AiStudioClient\src\components\ConvView\ScrollToBottomButton.tsx
import { useStickToBottomContext } from 'use-stick-to-bottom';
import { ArrowDown } from 'lucide-react';
import React, { useCallback, useEffect, useState } from 'react';

interface ScrollToBottomButtonProps {
  scrollContainerRef?: React.RefObject<HTMLDivElement>;
  chatSpaceWidth?: string;
}

// Custom comparison function for ScrollToBottomButton memoization
const areScrollButtonPropsEqual = (prevProps: ScrollToBottomButtonProps, nextProps: ScrollToBottomButtonProps) => {
  return (
    prevProps.scrollContainerRef === nextProps.scrollContainerRef &&
    prevProps.chatSpaceWidth === nextProps.chatSpaceWidth
  );
};

export const ScrollToBottomButton = React.memo(({ 
  scrollContainerRef,
  chatSpaceWidth = 'full'
}: ScrollToBottomButtonProps) => {
  
  // State for manual scroll position tracking when stick-to-bottom is disabled
  const [isAtBottomManual, setIsAtBottomManual] = useState(true);
  const [isHovered, setIsHovered] = useState(false);
  
  // Try to get context, but handle the case where it doesn't exist
  let stickToBottomContext = null;
  try {
      stickToBottomContext = useStickToBottomContext();
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
    if (!scrollContainerRef?.current) return;

    const element = scrollContainerRef.current;
    element.addEventListener('scroll', checkIfAtBottom, { passive: true });
    
    // Initial check
    checkIfAtBottom();
    
    return () => {
      element.removeEventListener('scroll', checkIfAtBottom);
    };
  }, [scrollContainerRef, checkIfAtBottom]);

  const handleScrollToBottom = () => {
   
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
    const isAtBottom = stickToBottomContext?.isAtBottom ?? true;

  // Only show the button when not at bottom
  if (isAtBottom) return null;

  return (
    <button
      className="absolute left-1/2 -translate-x-1/2 z-10 rounded-full p-2 shadow-md transition-all hover:shadow-lg ScrollToBottomButton"
      onClick={handleScrollToBottom}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      aria-label="Scroll to bottom"
      style={{
        bottom: '26px', // 16px (bottom-4) + 10px = 26px
        backgroundColor: isHovered ? 'var(--global-background-color)' : 'transparent',
        color: 'var(--global-primary-color)',
        border: '2px solid var(--global-primary-color)',
        borderRadius: '50%',
        width: '40px',
        height: '40px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        boxShadow: 'var(--global-box-shadow)',
        transition: 'background-color 0.2s ease'
      }}
    >
      <ArrowDown className="h-5 w-5" />
    </button>
  );
}, areScrollButtonPropsEqual);