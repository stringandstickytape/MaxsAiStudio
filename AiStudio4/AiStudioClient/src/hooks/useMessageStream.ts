// AiStudioClient\src\hooks\useMessageStream.ts
import { useState, useEffect } from 'react';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

export function useMessageStream(messageId: string, isStreamingTarget: boolean) {
  const [streamedContent, setStreamedContent] = useState('');
  const [isComplete, setIsComplete] = useState(false);

  useEffect(() => {
    console.log(`[useMessageStream] Hook called - messageId: ${messageId}, isStreamingTarget: ${isStreamingTarget}`);
    
    if (!isStreamingTarget) {
      console.log(`[useMessageStream] Not streaming target, clearing content for message: ${messageId}`);
      setStreamedContent('');
      setIsComplete(false);
      return;
    }

    console.log(`[useMessageStream] Setting up listeners for streaming message: ${messageId}`);

    const handleToken = (detail: any) => {
      console.log(`[useMessageStream] Received cfrag event:`, detail);
      if (detail.messageId === messageId) {
        console.log(`[useMessageStream] Adding content for message ${messageId}: "${detail.content}"`);
        setStreamedContent(prev => {
          const newContent = prev + detail.content;
          console.log(`[useMessageStream] Updated content for ${messageId}, total length: ${newContent.length}`);
          return newContent;
        });
      } else {
        console.log(`[useMessageStream] Ignoring content for different message. Expected: ${messageId}, Got: ${detail.messageId}`);
      }
    };

    const handleEnd = (detail: any) => {
      console.log(`[useMessageStream] Received endstream event:`, detail);
      if (detail.messageId === messageId) {
        console.log(`[useMessageStream] Stream ended for message: ${messageId}`);
        setIsComplete(true);
      }
    };

    const unsubscribeToken = listenToWebSocketEvent('cfrag', handleToken);
    const unsubscribeEnd = listenToWebSocketEvent('endstream', handleEnd);

    return () => {
      console.log(`[useMessageStream] Cleaning up listeners for message: ${messageId}`);
      unsubscribeToken();
      unsubscribeEnd();
    };
  }, [messageId, isStreamingTarget]);

  console.log(`[useMessageStream] Returning - messageId: ${messageId}, streamedContent length: ${streamedContent.length}, isComplete: ${isComplete}`);
  return { streamedContent, isComplete };
}