// AiStudioClient/src/stores/useWebSocketStore.ts
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
  activeStreamingMessageIds: Set<string>; // Track which messages are actively streaming

  

  setIsCancelling: (isCancelling: boolean) => void;
  setCurrentRequest: (request: { convId: string; messageId: string } | undefined) => void;
  addStreamingMessage: (messageId: string) => void;
  removeStreamingMessage: (messageId: string) => void;
  isMessageStreaming: (messageId: string) => boolean;
  hasActiveStreaming: () => boolean;
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
      set({ isCancelling: false, currentRequest: undefined, activeStreamingMessageIds: new Set() });
    });
    
    // Listen for streaming events to track active streaming messages
    listenToWebSocketEvent('cfrag', (detail) => {
      console.log(`[WebSocketStore] Received cfrag event:`, detail);
      if (detail.messageId) {
        console.log(`[WebSocketStore] Adding streaming message: ${detail.messageId}`);
        get().addStreamingMessage(detail.messageId);
      }
    });
    
    listenToWebSocketEvent('endstream', (detail) => {
      console.log(`[WebSocketStore] Received endstream event:`, detail);
      if (detail.messageId) {
        console.log(`[WebSocketStore] Removing streaming message: ${detail.messageId}`);
        get().removeStreamingMessage(detail.messageId);
      }
      set({ isCancelling: false });
    });
  }

  return {
    
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId(),
    lastMessageTime: null,
    reconnectAttempts: webSocketService.getReconnectAttempts(),
    isCancelling: false,
    currentRequest: undefined,
    activeStreamingMessageIds: new Set<string>(),

    

    setIsCancelling: (isCancelling) => {
      set({ isCancelling });
    },

    setCurrentRequest: (request) => {
      set({ currentRequest: request });
    },

    addStreamingMessage: (messageId) => {
      console.log(`[WebSocketStore] addStreamingMessage called for: ${messageId}`);
      set(state => {
        const newSet = new Set([...state.activeStreamingMessageIds, messageId]);
        console.log(`[WebSocketStore] Active streaming messages after add:`, Array.from(newSet));
        return { activeStreamingMessageIds: newSet };
      });
    },

    removeStreamingMessage: (messageId) => {
      console.log(`[WebSocketStore] removeStreamingMessage called for: ${messageId}`);
      set(state => {
        const newSet = new Set(state.activeStreamingMessageIds);
        newSet.delete(messageId);
        console.log(`[WebSocketStore] Active streaming messages after remove:`, Array.from(newSet));
        return { activeStreamingMessageIds: newSet };
      });
    },

    isMessageStreaming: (messageId) => {
      const state = get();
      const isStreaming = state.activeStreamingMessageIds.has(messageId);
      console.log(`[WebSocketStore] isMessageStreaming(${messageId}): ${isStreaming}, activeStreamingMessageIds:`, Array.from(state.activeStreamingMessageIds));
      return isStreaming;
    },

    hasActiveStreaming: () => {
      return get().activeStreamingMessageIds.size > 0;
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
