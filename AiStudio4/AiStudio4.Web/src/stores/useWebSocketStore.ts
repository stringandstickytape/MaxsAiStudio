
import { create } from 'zustand';
import { webSocketService, WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

interface WebSocketStore {
  
  isConnected: boolean;
  clientId: string | null;
  lastMessageTime: number | null;
  reconnectAttempts: number;
  isCancelling: boolean;
  currentRequest?: { convId: string; messageId: string };

  
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
  setIsCancelling: (isCancelling: boolean) => void;
  setCurrentRequest: (request: { convId: string; messageId: string } | undefined) => void;
}

export const useWebSocketStore = create<WebSocketStore>((set, get) => {
  
  if (typeof window !== 'undefined') {
    // Initialize connection status from WebSocketService
    webSocketService.onConnectionStatusChange((status) => {
      set({
        isConnected: status.isConnected,
        clientId: status.clientId,
        reconnectAttempts: webSocketService.getReconnectAttempts()
      });
    });
    
    // Set up periodic polling for lastMessageTime
    const updateLastMessageTime = () => {
      const time = webSocketService.getLastMessageTime();
      if (time !== get().lastMessageTime) {
        set({ lastMessageTime: time });
      }
    };
    
    const messageTimeInterval = setInterval(updateLastMessageTime, 1000);
    
    // Clean up interval when window is unloaded
    window.addEventListener('beforeunload', () => clearInterval(messageTimeInterval));
    
    // Listen for request cancellation
    listenToWebSocketEvent('request:cancelled', (detail) => {
      set({ isCancelling: false, currentRequest: undefined });
    });
  }

  return {
    
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId(),
    lastMessageTime: null,
    reconnectAttempts: webSocketService.getReconnectAttempts(),
    isCancelling: false,
    currentRequest: undefined,

    
    connect: () => {
      webSocketService.connect();
      // Connection status will be updated via the callback registered above
    },

    disconnect: () => {
      webSocketService.disconnect();
      // Connection status will be updated via the callback registered above
    },

    send: (messageType, content) => {
      webSocketService.send({ messageType, content });
      set({ lastMessageTime: Date.now() });
    },

    updateConnectionStatus: (status) => {
      // This is now handled by the onConnectionStatusChange callback
      // Keeping method for backward compatibility
      console.warn('updateConnectionStatus is deprecated - connection status is now managed automatically');
    },

    updateLastMessageTime: (time) => {
      set({ lastMessageTime: time });
    },

    // Simplified methods that delegate to the store state
    // These remain for backward compatibility
    setConnected: (isConnected) => {
      set({ isConnected });
      console.warn('setConnected is deprecated - connection status is now managed automatically');
    },

    setClientId: (clientId) => {
      set({ clientId });
      console.warn('setClientId is deprecated - connection status is now managed automatically');
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

    setIsCancelling: (isCancelling) => {
      set({ isCancelling });
    },

    setCurrentRequest: (request) => {
      set({ currentRequest: request });
    },
  };
});


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


(window as any).debugWebSocketStore = debugWebSocketStore;

