﻿// AiStudioClient/src/stores/useWebSocketStore.ts
import { create } from 'zustand';
import { webSocketService, WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

interface StreamingContentInfo {
  content: string;
  previousLength: number;
  animationKey: number;
}

interface WebSocketStore {
  
  isConnected: boolean;
  clientId: string | null;
  lastMessageTime: number | null;
  reconnectAttempts: number;
  isCancelling: boolean;
  currentRequest?: { convId: string; messageId: string };
  activeStreamingMessageIds: Set<string>; // Track which messages are actively streaming
  streamingContent: Map<string, StreamingContentInfo>; // Store streaming content with animation info

  

  setIsCancelling: (isCancelling: boolean) => void;
  setCurrentRequest: (request: { convId: string; messageId: string } | undefined) => void;
  addStreamingMessage: (messageId: string) => void;
  removeStreamingMessage: (messageId: string) => void;
  isMessageStreaming: (messageId: string) => boolean;
  hasActiveStreaming: () => boolean;
  updateStreamingContent: (messageId: string, content: string) => void;
  getStreamingContent: (messageId: string) => StreamingContentInfo | undefined;
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
      if (detail.messageId) {
        //console.log(`🔔 WebSocketStore received cfrag for ${detail.messageId}: "${detail.content}"`);
        get().addStreamingMessage(detail.messageId);
        
        // Get current content and append the new fragment
        const currentInfo = get().streamingContent.get(detail.messageId);
        const fullContent = (currentInfo?.content || '') + detail.content;
        get().updateStreamingContent(detail.messageId, fullContent);
      }
    });
    
    listenToWebSocketEvent('endstream', (detail) => {
      if (detail.messageId) {
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
    streamingContent: new Map<string, StreamingContentInfo>(),

    

    setIsCancelling: (isCancelling) => {
      set({ isCancelling });
    },

    setCurrentRequest: (request) => {
      set({ currentRequest: request });
    },

    addStreamingMessage: (messageId) => {
      set(state => {
        const newSet = new Set([...state.activeStreamingMessageIds, messageId]);
        return { activeStreamingMessageIds: newSet };
      });
    },

    removeStreamingMessage: (messageId) => {
      set(state => {
        const newSet = new Set(state.activeStreamingMessageIds);
        newSet.delete(messageId);
        
        // Clean up streaming content when streaming ends
        const newMap = new Map(state.streamingContent);
        newMap.delete(messageId);
        
        return { 
          activeStreamingMessageIds: newSet,
          streamingContent: newMap
        };
      });
    },

    isMessageStreaming: (messageId) => {
      const state = get();
      const isStreaming = state.activeStreamingMessageIds.has(messageId);
      return isStreaming;
    },

    hasActiveStreaming: () => {
      return get().activeStreamingMessageIds.size > 0;
    },

    updateStreamingContent: (messageId: string, content: string) => {
      set(state => {
        const newMap = new Map(state.streamingContent);
        const existing = newMap.get(messageId);
        const previousLength = existing?.content.length || 0;
        
        const newInfo = {
          content,
          previousLength,
          animationKey: existing ? existing.animationKey + 1 : 1
        };
        
        //console.log(`💾 Updating streaming content for ${messageId}: previousLength=${previousLength}, animationKey=${newInfo.animationKey}, contentLength=${content.length}`);
        
        newMap.set(messageId, newInfo);
        
        return { streamingContent: newMap };
      });
    },

    getStreamingContent: (messageId: string) => {
      return get().streamingContent.get(messageId);
    },
  };
});


export const debugWebSocketStore = () => {
  const state = useWebSocketStore.getState();
  return state;
};


(window as any).debugWebSocketStore = debugWebSocketStore;
