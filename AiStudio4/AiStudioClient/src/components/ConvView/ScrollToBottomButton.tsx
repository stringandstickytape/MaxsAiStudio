// AiStudioClient\src\components\ConvView\ScrollToBottomButton.tsx
import { useStickToBottomContext } from 'use-stick-to-bottom';
import { ArrowDown } from 'lucide-react';

interface ScrollToBottomButtonProps {
  onActivateSticking: () => void;
}

export const ScrollToBottomButton = ({ onActivateSticking }: ScrollToBottomButtonProps) => {
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
      className="absolute rounded-full p-2 bg-primary hover:bg-primary/90 text-primary-foreground shadow-md right-4 bottom-4 z-10"
      onClick={handleScrollToBottom}
      aria-label="Scroll to bottom"
      style={{
        backgroundColor: 'var(--convview-accent-color, #2563eb)',
        color: 'var(--convview-text-color, #ffffff)',
        ...(window?.theme?.ConvView?.style || {})
      }}
    >
      <ArrowDown className="h-5 w-5" />
    </button>
  );
};