// src/hooks/useWebSocket.ts
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

/**
 * Hook for working with WebSocket connections, using Zustand for state management
 */
export function useWebSocket(options: UseWebSocketOptions = {}): UseWebSocketResult {
  const { autoConnect = true, subscriptions = {} } = options;

  // Use the Zustand store for WebSocket state
  const { connect, disconnect, send } = useWebSocketStore();

  // Use the WebSocketStatus hook to track connection state
  const { isConnected, clientId } = useWebSocketStatus();

  // Set up subscriptions
  useEffect(() => {
    // Subscribe to all message types in the subscriptions object
    Object.entries(subscriptions).forEach(([messageType, handler]) => {
      webSocketService.subscribe(messageType, handler);
    });

    // Clean up subscriptions on unmount
    return () => {
      Object.entries(subscriptions).forEach(([messageType, handler]) => {
        webSocketService.unsubscribe(messageType, handler);
      });
    };
  }, [subscriptions]);

  // Auto-connect if enabled
  useEffect(() => {
    if (autoConnect) {
      connect();
    }

    // We don't automatically disconnect to avoid disconnecting when a component using
    // this hook unmounts but other components still need the connection
  }, [autoConnect, connect]);

  return {
    isConnected,
    clientId,
    connect,
    disconnect,
    send,
  };
}
