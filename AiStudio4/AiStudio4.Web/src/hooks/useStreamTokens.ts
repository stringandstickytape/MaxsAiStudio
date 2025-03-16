
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';

import { useCallback, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

export function useStreamTokens() {
  const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: true });
  const { isCancelling } = useWebSocketStore();
  
  // Reset tokens when cancellation is complete
  useEffect(() => {
    if (!isCancelling && streamTokens.length > 0) {
      const timeout = setTimeout(() => {
        const event = new CustomEvent('request:cancelled', { detail: { cancelled: true } });
        window.dispatchEvent(event);
      }, 500);
      return () => clearTimeout(timeout);
    }
  }, [isCancelling, streamTokens.length]);

  useEffect(() => {
    const handleCancelled = () => reset();
    window.addEventListener('request:cancelled', handleCancelled);
    return () => window.removeEventListener('request:cancelled', handleCancelled);
  }, [reset]);

  return { streamTokens, resetStreamTokens: reset };
}

