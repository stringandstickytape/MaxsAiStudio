import { ConvView } from './ConvView';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
  isCancelling?: boolean;
}

export function ChatContainer({ isMobile, streamTokens, isCancelling }: ChatContainerProps) {
  
  return (
    <div className="h-full w-full overflow-hidden">
      <ConvView streamTokens={streamTokens} isCancelling={isCancelling} />
    </div>
  );
}
