import { useEffect, useRef } from 'react';
import { ConvView } from './ConvView';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
}

export function ChatContainer({ isMobile, streamTokens }: ChatContainerProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (messagesEndRef.current) {
      requestAnimationFrame(() => {
        if (messagesEndRef.current) {
          messagesEndRef.current.scrollTo({
            top: messagesEndRef.current.scrollHeight,
            behavior: 'smooth',
          });
        }
      });
    }
  }, [streamTokens]);

  return (
    <div ref={messagesEndRef} className="h-full w-full overflow-y-auto p-4 scroll-smooth">
      <ConvView streamTokens={streamTokens} />
    </div>
  );
}
