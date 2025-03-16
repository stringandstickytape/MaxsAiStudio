
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

    
    listenToWebSocketEvent('message:received', (detail) => {
      set({ lastMessageTime: detail.timestamp || Date.now() });
    });
  }

  // Listen for request cancelled events
  listenToWebSocketEvent('request:cancelled', (detail) => {
    set({ isCancelling: false, currentRequest: undefined });
  });

  return {
    
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId(),
    lastMessageTime: null,
    reconnectAttempts: webSocketService.getReconnectAttempts(),
    isCancelling: false,
    currentRequest: undefined,

    
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

