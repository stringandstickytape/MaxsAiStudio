// AiStudioClient\src\components\ConvView\ScrollManager.tsx
import { useEffect, useRef } from 'react';
import { useStickToBottomContext } from 'use-stick-to-bottom';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { WindowEvents } from '@/services/windowEvents';

interface ScrollManagerProps {
  isStreaming: boolean;
  streamTokens: string[];
}

export const ScrollManager = ({ isStreaming, streamTokens }: ScrollManagerProps) => {
  const { isAtBottom, scrollToBottom } = useStickToBottomContext();
  const { jumpToEndEnabled, setJumpToEndEnabled } = useJumpToEndStore();
  
  // Use a ref to track the last isAtBottom value to reduce state updates
  const lastIsAtBottomRef = useRef(isAtBottom);
  
  // Expose the scrollToBottom function and isAtBottom state globally
  useEffect(() => {
    // Define a global function to handle scroll to bottom requests
    window.scrollConversationToBottom = () => {
      scrollToBottom();
      setJumpToEndEnabled(true);
      return true;
    };
    
    // Define a global function to check if we're at the bottom
    window.getScrollBottomState = () => {
      return isAtBottom;
    };
    
    return () => {
      // Clean up the global functions when component unmounts
      delete window.scrollConversationToBottom;
      delete window.getScrollBottomState;
    };
  }, [scrollToBottom, setJumpToEndEnabled, isAtBottom]);
  
  // Update jumpToEndEnabled when user manually scrolls, but with debouncing
  useEffect(() => {
    // Skip frequent updates during streaming to improve performance
    if (isStreaming && streamTokens.length > 0 && lastIsAtBottomRef.current === isAtBottom) {
      return;
    }
    
    // Update the ref
    lastIsAtBottomRef.current = isAtBottom;
    
    // Use a timeout to debounce the state updates
    const timeoutId = setTimeout(() => {
      // When we detect we're at the bottom, update jumpToEndEnabled to true
      if (isAtBottom && !jumpToEndEnabled) {
        setJumpToEndEnabled(true);
      }
      // When we detect we're not at the bottom, update jumpToEndEnabled to false
      else if (!isAtBottom && jumpToEndEnabled) {
        setJumpToEndEnabled(false);
      }
    }, 200);
    
    return () => clearTimeout(timeoutId);
  }, [isAtBottom, jumpToEndEnabled, setJumpToEndEnabled, isStreaming, streamTokens.length]);
  
  // Listen for scroll-to-bottom events from other components
  useEffect(() => {
    // Create a function that will be called when the SCROLL_TO_BOTTOM event is triggered
    const handleScrollToBottom = () => {
      // Set jumpToEndEnabled to true
      setJumpToEndEnabled(true);
      
      // Try to scroll to bottom
      scrollToBottom();
    };
    
    window.addEventListener(WindowEvents.SCROLL_TO_BOTTOM, handleScrollToBottom);
    return () => {
      window.removeEventListener(WindowEvents.SCROLL_TO_BOTTOM, handleScrollToBottom);
    };
  }, [setJumpToEndEnabled, scrollToBottom]);
  
  return null;
};