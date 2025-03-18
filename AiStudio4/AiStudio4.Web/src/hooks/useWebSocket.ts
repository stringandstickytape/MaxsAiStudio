
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

  // Subscribe to connection status changes directly from WebSocketService  
  useEffect(() => {
    const unsubscribe = webSocketService.onConnectionStatusChange(setConnectionStatus);
    return () => unsubscribe && webSocketService.offConnectionStatusChange(setConnectionStatus);
  }, []);
  
  // Subscribe to message handlers
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

  // Handle auto-connect
  useEffect(() => {
    if (autoConnect) {
      webSocketService.connect();
    }
  }, [autoConnect]);

  // Return simplified interface that delegates directly to WebSocketService
  return {
    isConnected: connectionStatus.isConnected,
    clientId: connectionStatus.clientId,
    connect: webSocketService.connect,
    disconnect: webSocketService.disconnect,
    send: (messageType: string, content: any) => webSocketService.send({ messageType, content })
  };
}

