// AiStudioClient\src\components\ConvView\ScrollToBottomButton.tsx
import { useStickToBottomContext } from 'use-stick-to-bottom';
import { ArrowDown } from 'lucide-react';
import React from 'react';

interface ScrollToBottomButtonProps {
  onActivateSticking: () => void;
}

// Custom comparison function for ScrollToBottomButton memoization
const areScrollButtonPropsEqual = (prevProps: ScrollToBottomButtonProps, nextProps: ScrollToBottomButtonProps) => {
  // The onActivateSticking callback should be stable, but even if it's not,
  // the component's behavior doesn't change based on the callback content
  // We'll assume the callback is functionally equivalent between renders
  return true;
};

export const ScrollToBottomButton = React.memo(({ onActivateSticking }: ScrollToBottomButtonProps) => {
  const { isAtBottom, scrollToBottom } = useStickToBottomContext();

  const handleScrollToBottom = () => {
    // Enable sticking to bottom when button is clicked
    onActivateSticking();
    // Scroll to bottom
    scrollToBottom();
  };

  // Only show the button when not at bottom
  if (isAtBottom) return null;

  return (
    <button
      className="absolute rounded-full p-2 shadow-md right-4 bottom-4 z-10 transition-colors ScrollToBottomButton"
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