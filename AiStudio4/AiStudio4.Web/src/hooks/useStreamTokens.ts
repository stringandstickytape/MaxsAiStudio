
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';


export function useStreamTokens() {
  const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: true });

  return { streamTokens, resetStreamTokens: reset };
}

