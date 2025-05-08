
import { useEffect, useState } from 'react';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';

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

    // Status message handler
    const { setMessage } = useStatusMessageStore.getState();
      const handleStatusMessage = (data: any) => {
      setMessage(data?.message || '');
    };
    webSocketService.subscribe('status', handleStatusMessage);

    return () => {
      Object.entries(subscriptions).forEach(([messageType, handler]) => {
        webSocketService.unsubscribe(messageType, handler);
      });
      webSocketService.unsubscribe('status', handleStatusMessage);
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

