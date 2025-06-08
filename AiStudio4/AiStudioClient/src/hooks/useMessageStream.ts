// AiStudioClient\src\hooks\useMessageStream.ts
import { useState, useEffect } from 'react';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

export function useMessageStream(messageId: string, isStreamingTarget: boolean) {
  const [streamedContent, setStreamedContent] = useState('');
  const [isComplete, setIsComplete] = useState(false);

  useEffect(() => {
    if (!isStreamingTarget) {
      setStreamedContent('');
      setIsComplete(false);
      return;
    }

    const handleToken = (detail: any) => {
      if (detail.messageId === messageId) {
        setStreamedContent(prev => prev + detail.content);
      }
    };

    const handleEnd = (detail: any) => {
      if (detail.messageId === messageId) {
        setIsComplete(true);
      }
    };

    const unsubscribeToken = listenToWebSocketEvent('cfrag', handleToken);
    const unsubscribeEnd = listenToWebSocketEvent('endstream', handleEnd);

    return () => {
      unsubscribeToken();
      unsubscribeEnd();
    };
  }, [messageId, isStreamingTarget]);

  return { streamedContent, isComplete };
}