import { ConvView } from './ConvView';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
// StickToBottom removed

interface ChatContainerProps {
  isMobile: boolean;
  isCancelling?: boolean;
}

export function ChatContainer({ isMobile, isCancelling }: ChatContainerProps) {
  
  const { isCancelling: wsIsCancelling } = useWebSocketStore();
  
  return (
      <div className="h-full w-full overflow-hidden">
          <ConvView 
            isCancelling={isCancelling || wsIsCancelling}
          />
    </div>
  );
}