// AiStudio4.Web/src/stores/useWebSocketStore.ts
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

  

  setIsCancelling: (isCancelling: boolean) => void;
  setCurrentRequest: (request: { convId: string; messageId: string } | undefined) => void;
}

export const useWebSocketStore = create<WebSocketStore>((set, get) => {
  
  if (typeof window !== 'undefined') {
    
    webSocketService.onConnectionStatusChange((status) => {
      // Compare incoming status with current state to prevent unnecessary updates
      const currentState = get();
      const currentAttempts = webSocketService.getReconnectAttempts(); // Get fresh attempts count
      if (
        currentState.isConnected !== status.isConnected ||
        currentState.clientId !== status.clientId ||
        currentState.reconnectAttempts !== currentAttempts
      ) {
        set({
          isConnected: status.isConnected,
          clientId: status.clientId,
          reconnectAttempts: currentAttempts,
        });
      }
    });
    
    
    const updateLastMessageTime = () => {
      const time = webSocketService.getLastMessageTime();
      if (time !== get().lastMessageTime) {
        set({ lastMessageTime: time });
      }
    };
    
    const messageTimeInterval = setInterval(updateLastMessageTime, 1000);
    
    
    window.addEventListener('beforeunload', () => clearInterval(messageTimeInterval));
    
    
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
