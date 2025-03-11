// src/stores/useWebSocketStore.ts
import { create } from 'zustand';
import { webSocketService, WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

interface WebSocketStore {
  // State
  isConnected: boolean;
  clientId: string | null;
  lastMessageTime: number | null;
  reconnectAttempts: number;

  // Actions
  connect: () => void;
  disconnect: () => void;
  send: (messageType: string, content: any) => void;
  updateConnectionStatus: (status: WebSocketConnectionStatus) => void;
  updateLastMessageTime: (time: number) => void;
  setConnected: (isConnected: boolean) => void;
  setClientId: (clientId: string) => void;
  setReconnectAttempts: (attempts: number) => void;
  incrementReconnectAttempts: () => void;
  resetReconnectAttempts: () => void;
}

export const useWebSocketStore = create<WebSocketStore>((set, get) => {
  // Set up event listeners for WebSocket events
  if (typeof window !== 'undefined') {
    // Listen for connection status changes
    listenToWebSocketEvent('connection:status', (detail) => {
      if (detail.type === 'connected') {
        set({
          isConnected: true,
          reconnectAttempts: 0,
        });
      } else if (detail.type === 'disconnected') {
        set({ isConnected: false });
      } else if (detail.type === 'clientId' && detail.clientId) {
        set({ clientId: detail.clientId });
      }
    });

    // Listen for all messages to update lastMessageTime
    listenToWebSocketEvent('message:received', (detail) => {
      set({ lastMessageTime: detail.timestamp || Date.now() });
    });
  }

  return {
    // Initial state
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId(),
    lastMessageTime: null,
    reconnectAttempts: webSocketService.getReconnectAttempts(),

    // Actions
    connect: () => {
      webSocketService.connect();
    },

    disconnect: () => {
      webSocketService.disconnect();
    },

    send: (messageType, content) => {
      webSocketService.send({ messageType, content });
      set({ lastMessageTime: Date.now() });
    },

    updateConnectionStatus: (status) => {
      set({
        isConnected: status.isConnected,
        clientId: status.clientId,
      });
    },

    updateLastMessageTime: (time) => {
      set({ lastMessageTime: time });
    },

    setConnected: (isConnected) => {
      set({ isConnected });
    },

    setClientId: (clientId) => {
      set({ clientId });
    },

    setReconnectAttempts: (attempts) => {
      set({ reconnectAttempts: attempts });
    },

    incrementReconnectAttempts: () => {
      set((state) => ({ reconnectAttempts: state.reconnectAttempts + 1 }));
    },

    resetReconnectAttempts: () => {
      set({ reconnectAttempts: 0 });
    },
  };
});

// Debug helper for console
export const debugWebSocketStore = () => {
  const state = useWebSocketStore.getState();
  console.group('WebSocket Store Debug');
  console.log('Connected:', state.isConnected);
  console.log('Client ID:', state.clientId);
  console.log('Last Message Time:', state.lastMessageTime ? new Date(state.lastMessageTime).toISOString() : 'Never');
  console.log('Reconnect Attempts:', state.reconnectAttempts);
  console.groupEnd();
  return state;
};

// Export for console access
(window as any).debugWebSocketStore = debugWebSocketStore;
