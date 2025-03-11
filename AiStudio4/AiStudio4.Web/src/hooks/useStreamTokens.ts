// src/hooks/useStreamTokens.ts
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';

/**
 * Hook for working with streaming tokens from the WebSocket
 */
export function useStreamTokens() {
  const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: true });

  return { streamTokens, resetStreamTokens: reset };
}
