// src/stores/useWebSocketStore.ts
import { create } from 'zustand';
import { webSocketService, WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { useEffect } from 'react';

interface WebSocketStore {
  // State
  isConnected: boolean;
  clientId: string | null;
  lastMessageTime: number | null;
  
  // Actions
  connect: () => void;
  disconnect: () => void;
  send: (messageType: string, content: any) => void;
  updateConnectionStatus: (status: WebSocketConnectionStatus) => void;
  updateLastMessageTime: (time: number) => void;
}

export const useWebSocketStore = create<WebSocketStore>((set, get) => {
  // Set up event listeners
  // Note: This is a bit unusual but works with Zustand
  if (typeof window !== 'undefined') {
    // Listen for connection status changes
    listenToWebSocketEvent('connection:status', (detail) => {
      if (detail.content) {
        set({
          isConnected: detail.content.isConnected,
          clientId: detail.content.clientId || detail.clientId
        });
      }
    });
    
    // Listen for all messages to update lastMessageTime
    listenToWebSocketEvent('message:received', (detail) => {
      set({ lastMessageTime: detail.timestamp });
    });
  }
  
  return {
    // Initial state
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId(),
    lastMessageTime: null,
    
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
        clientId: status.clientId
      });
    },
    
    updateLastMessageTime: (time) => {
      set({ lastMessageTime: time });
    }
  };
});

// Debug helper for console
export const debugWebSocketStore = () => {
  const state = useWebSocketStore.getState();
  console.group('WebSocket Store Debug');
  console.log('Connected:', state.isConnected);
  console.log('Client ID:', state.clientId);
  console.log('Last Message Time:', state.lastMessageTime ? new Date(state.lastMessageTime).toISOString() : 'Never');
  console.groupEnd();
  return state;
};

// Export for console access
(window as any).debugWebSocketStore = debugWebSocketStore;