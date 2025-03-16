import { ConvView } from './ConvView';
import { useStreamTokens } from '@/hooks/useStreamTokens';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
  isCancelling?: boolean;
}

export function ChatContainer({ isMobile, streamTokens, isCancelling }: ChatContainerProps) {
  // Get enhanced streaming state from hook
  const { isStreaming, lastStreamedContent } = useStreamTokens();
  
  return (
    <div className="h-full w-full overflow-hidden">
      <ConvView 
        streamTokens={streamTokens} 
        isCancelling={isCancelling}
        isStreaming={isStreaming}
        lastStreamedContent={lastStreamedContent}
      />
    </div>
  );
}