// AiStudioClient\src\hooks\useMessageStream.ts
import { useState, useEffect } from 'react';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

export function useMessageStream(messageId: string, isStreamingTarget: boolean) {
  const [isComplete, setIsComplete] = useState(false);
  
  // Subscribe to the specific streaming content for this message
  const streamingContentInfo = useWebSocketStore(state => 
    state.streamingContent.get(messageId)
  );

  useEffect(() => {
    if (!isStreamingTarget) {
      setIsComplete(false);
      return;
    }

    const handleEnd = (detail: any) => {
      if (detail.messageId === messageId) {
        setIsComplete(true);
      }
    };

    const unsubscribeEnd = listenToWebSocketEvent('endstream', handleEnd);

    return () => {
      unsubscribeEnd();
    };
  }, [messageId, isStreamingTarget]);
  
  return { 
    streamedContent: streamingContentInfo?.content || '', 
    isComplete, 
    newContentInfo: {
      previousLength: streamingContentInfo?.previousLength || 0,
      animationKey: streamingContentInfo?.animationKey || 0
    }
  };
}