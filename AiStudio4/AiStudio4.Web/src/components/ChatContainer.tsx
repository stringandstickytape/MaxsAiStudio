import { useEffect, useRef, useState } from 'react';
import { ConvView } from './ConvView';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
}

export function ChatContainer({ isMobile, streamTokens }: ChatContainerProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
  const [lastScrollHeight, setLastScrollHeight] = useState(0);
  const [lastMessageCount, setLastMessageCount] = useState(0);

  // Auto-scroll when new stream tokens appear
  useEffect(() => {
    if (shouldAutoScroll && messagesEndRef.current) {
      // Use requestAnimationFrame for smoother scrolling
      requestAnimationFrame(() => {
        if (messagesEndRef.current) {
          const currentScrollHeight = messagesEndRef.current.scrollHeight;
          messagesEndRef.current.scrollTo({
            top: currentScrollHeight,
            behavior: 'smooth',
          });
          setLastScrollHeight(currentScrollHeight);
        }
      });
    }
  }, [streamTokens, shouldAutoScroll]);

  // Detect if user has manually scrolled up
  const handleScroll = () => {
    if (!messagesEndRef.current) return;
    
    const { scrollTop, scrollHeight, clientHeight } = messagesEndRef.current;
    const isAtBottom = scrollTop + clientHeight >= scrollHeight - 50;
    
    // Only change auto-scroll state when user manually scrolls
    if (scrollHeight > lastScrollHeight) {
      setLastScrollHeight(scrollHeight);
    } else {
      setShouldAutoScroll(isAtBottom);
    }
  };

  return (
    <div 
      ref={messagesEndRef} 
      className="h-full w-full overflow-y-auto scroll-smooth"
      onScroll={handleScroll}
    >
      <ConvView streamTokens={streamTokens} />
    </div>
  );
}
