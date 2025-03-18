
import { useEffect, useState } from 'react';
import { webSocketService } from '@/services/websocket/WebSocketService';

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
  const [connectionStatus, setConnectionStatus] = useState<WebSocketConnectionStatus>({
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId()
  });

  
  useEffect(() => {
    const unsubscribe = webSocketService.onConnectionStatusChange(setConnectionStatus);
    return () => unsubscribe && webSocketService.offConnectionStatusChange(setConnectionStatus);
  }, []);
  
  
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
      webSocketService.connect();
    }
  }, [autoConnect]);

  
  return {
    isConnected: connectionStatus.isConnected,
    clientId: connectionStatus.clientId,
    connect: webSocketService.connect,
    disconnect: webSocketService.disconnect,
    send: (messageType: string, content: any) => webSocketService.send({ messageType, content })
  };
}

