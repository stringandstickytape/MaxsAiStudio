// AiStudioClient\src\hooks\useMessageStream.ts
import { useState, useEffect, useRef } from 'react';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

export function useMessageStream(messageId: string, isStreamingTarget: boolean) {
  const [streamedContent, setStreamedContent] = useState('');
  const [isComplete, setIsComplete] = useState(false);
  
  // Buffering state
  const bufferRef = useRef<string>('');
  const lastUpdateRef = useRef<number>(0);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    
    if (!isStreamingTarget) {
      setStreamedContent('');
      setIsComplete(false);
      // Clear buffer and timeout when not streaming
      bufferRef.current = '';
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
      return;
    }

    const flushBuffer = () => {
      if (bufferRef.current) {
        setStreamedContent(bufferRef.current);
        lastUpdateRef.current = Date.now();
      }
    };

    const scheduleUpdate = () => {
      if (timeoutRef.current) return; // Already scheduled
      
      const now = Date.now();
      const timeSinceLastUpdate = now - lastUpdateRef.current;
      const delay = Math.max(0, 50 - timeSinceLastUpdate); // 50ms throttle
      
      timeoutRef.current = setTimeout(() => {
        flushBuffer();
        timeoutRef.current = null;
      }, delay);
    };

    const handleToken = (detail: any) => {
      if (detail.messageId === messageId) {
        // Add to buffer
        bufferRef.current += detail.content;
        
        // Schedule an update (throttled to 1 second)
        scheduleUpdate();
      }
    };

    const handleEnd = (detail: any) => {
      if (detail.messageId === messageId) {
        // Clear any pending timeout and flush final buffer content
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
          timeoutRef.current = null;
        }
        flushBuffer();
        setIsComplete(true);
      }
    };

    const unsubscribeToken = listenToWebSocketEvent('cfrag', handleToken);
    const unsubscribeEnd = listenToWebSocketEvent('endstream', handleEnd);

    return () => {
      unsubscribeToken();
      unsubscribeEnd();
      // Clear any pending timeout on cleanup
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
    };
  }, [messageId, isStreamingTarget]);

  return { streamedContent, isComplete };
}