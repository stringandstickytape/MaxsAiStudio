import { ConvView } from './ConvView';

interface ChatContainerProps {
  streamTokens: string[];
  isMobile: boolean;
}

export function ChatContainer({ isMobile, streamTokens }: ChatContainerProps) {
  
  return (
    <div className="h-full w-full overflow-hidden">
      <ConvView streamTokens={streamTokens} />
    </div>
  );
}
