// AiStudioClient\src\components\ConvView\ScrollManager.tsx
import { useEffect, useRef, useCallback } from 'react';
// useStickToBottomContext removed
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { WindowEvents } from '@/services/windowEvents';

// Add this to the global Window interface
declare global {
  interface Window {
    scrollCheckTimeout?: NodeJS.Timeout;
    scrollConversationToBottom?: () => boolean;
    getScrollBottomState?: () => boolean;
  }
}

interface ScrollManagerProps {
  isStreaming: boolean;
  streamTokens: string[];
}

export const ScrollManager = ({ isStreaming, streamTokens }: ScrollManagerProps) => {
  const { jumpToEndEnabled, setJumpToEndEnabled } = useJumpToEndStore();
  
  // Create refs for scroll management
  const scrollContainerRef = useRef<HTMLDivElement | null>(null);
  const isAtBottomRef = useRef(true);
  
  // Function to check if we're at the bottom
  const checkIfAtBottom = useCallback(() => {
    const container = document.querySelector('.ConvView');
    if (!container) return true;
    
    const atBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 20;
    isAtBottomRef.current = atBottom;
    return atBottom;
  }, []);
  
  // Function to scroll to bottom
  const scrollToBottom = useCallback(() => {
    const container = document.querySelector('.ConvView');
    if (!container) return;
    
    container.scrollTop = container.scrollHeight;
    isAtBottomRef.current = true;
  }, []);
  
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
      return checkIfAtBottom();
    };
    
    return () => {
      // Clean up the global functions when component unmounts
      delete window.scrollConversationToBottom;
      delete window.getScrollBottomState;
    };
  }, [scrollToBottom, setJumpToEndEnabled, checkIfAtBottom]);
  
  // Update jumpToEndEnabled when user manually scrolls, but with debouncing
  useEffect(() => {
    // Function to check scroll position and update state
    const checkScrollPosition = () => {
      const isAtBottom = checkIfAtBottom();
      
      // When we detect we're at the bottom, update jumpToEndEnabled to true
      if (isAtBottom && !jumpToEndEnabled) {
        setJumpToEndEnabled(true);
      }
      // When we detect we're not at the bottom, update jumpToEndEnabled to false
      else if (!isAtBottom && jumpToEndEnabled) {
        setJumpToEndEnabled(false);
      }
    };
    
    // Set up an interval to periodically check scroll position
    const intervalId = setInterval(checkScrollPosition, 200);
    
    // Also add a scroll event listener to the container
    const container = document.querySelector('.ConvView');
    if (container) {
      container.addEventListener('scroll', () => {
        // Clear any existing timeout to debounce
        if (window.scrollCheckTimeout) {
          clearTimeout(window.scrollCheckTimeout);
        }
        
        // Set a new timeout
        window.scrollCheckTimeout = setTimeout(checkScrollPosition, 100);
      });
    }
    
    return () => {
      clearInterval(intervalId);
      if (container) {
        container.removeEventListener('scroll', checkScrollPosition);
      }
      if (window.scrollCheckTimeout) {
        clearTimeout(window.scrollCheckTimeout);
      }
    };
  }, [checkIfAtBottom, jumpToEndEnabled, setJumpToEndEnabled]);
  
  // Listen for scroll-to-bottom events from other components
  useEffect(() => {
    // Create a function that will be called when the SCROLL_TO_BOTTOM event is triggered
    const handleScrollToBottom = () => {
      // Set jumpToEndEnabled to true
      setJumpToEndEnabled(true);
      
      // Try to scroll to bottom
      scrollToBottom();
    };
    
    // Listen for jump-to-end events from the streaming hook
    const handleJumpToEnd = () => {
      if (jumpToEndEnabled) {
        scrollToBottom();
      }
    };
    
    window.addEventListener(WindowEvents.SCROLL_TO_BOTTOM, handleScrollToBottom);
    window.addEventListener('jump-to-end', handleJumpToEnd);
    
    return () => {
      window.removeEventListener(WindowEvents.SCROLL_TO_BOTTOM, handleScrollToBottom);
      window.removeEventListener('jump-to-end', handleJumpToEnd);
    };
  }, [setJumpToEndEnabled, scrollToBottom, jumpToEndEnabled]);
  
  return null;
};