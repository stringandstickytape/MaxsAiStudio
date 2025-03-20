import { ConvView } from './ConvView';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { StickToBottom } from 'use-stick-to-bottom';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
  isCancelling?: boolean;
}

export function ChatContainer({ isMobile, streamTokens, isCancelling }: ChatContainerProps) {
  
  const { isStreaming, lastStreamedContent } = useStreamTokens();
  
  return (
      <div className="h-full w-full overflow-hidden">
          <StickToBottom className="h-full relative" resize="smooth" initial="smooth" stiffness={0.03} damping={0.15} mass={0.85}>
        <StickToBottom.Content className="flex flex-col h-full">
          <ConvView 
            streamTokens={streamTokens} 
            isCancelling={isCancelling}
            isStreaming={isStreaming}
            lastStreamedContent={lastStreamedContent}
          />
        </StickToBottom.Content>
      </StickToBottom>
    </div>
  );
}