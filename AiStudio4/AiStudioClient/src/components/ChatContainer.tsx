import React from 'react';
import { ConvView } from './ConvView';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { TipOfTheDayOverlay } from './TipOfTheDayOverlay';
// StickToBottom removed

interface ChatContainerProps {
  isMobile: boolean;
  isCancelling?: boolean;
}

// Custom comparison function for ChatContainer memoization
const areChatContainerPropsEqual = (prevProps: ChatContainerProps, nextProps: ChatContainerProps) => {
  return (
    prevProps.isMobile === nextProps.isMobile &&
    prevProps.isCancelling === nextProps.isCancelling
  );
};

export const ChatContainer = React.memo(({ isMobile, isCancelling }: ChatContainerProps) => {
  
  const { isCancelling: wsIsCancelling } = useWebSocketStore();
  
  return (
      <div className="h-full w-full overflow-hidden relative">
          <ConvView 
            isCancelling={isCancelling || wsIsCancelling}
          />
          <TipOfTheDayOverlay />
    </div>
  );
}, areChatContainerPropsEqual);