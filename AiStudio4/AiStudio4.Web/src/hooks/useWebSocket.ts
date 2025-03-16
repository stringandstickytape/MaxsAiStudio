
import { useEffect } from 'react';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useWebSocketStatus } from '@/utils/webSocketUtils';

interface UseWebSocketOptions {
  autoConnect?: boolean;
  subscriptions?: { [key: string]: (data: any) => void };
}

interface UseWebSocketResult {
  isConnected: boolean;
  clientId: string | null;
  connect: () => void;
  disconnect: () => void;
  send: (messageType: string, content: any) => void;
}


export function useWebSocket(options: UseWebSocketOptions = {}): UseWebSocketResult {
  const { autoConnect = true, subscriptions = {} } = options;

  
  const { connect, disconnect, send } = useWebSocketStore();

  
  const { isConnected, clientId } = useWebSocketStatus();

  
  useEffect(() => {
    
    Object.entries(subscriptions).forEach(([messageType, handler]) => {
      webSocketService.subscribe(messageType, handler);
    });

    
    return () => {
      Object.entries(subscriptions).forEach(([messageType, handler]) => {
        webSocketService.unsubscribe(messageType, handler);
      });
    };
  }, [subscriptions]);

  
  useEffect(() => {
    if (autoConnect) {
      connect();
    }

    
    
  }, [autoConnect, connect]);

  return {
    isConnected,
    clientId,
    connect,
    disconnect,
    send,
  };
}

