import { useState, useEffect, useCallback } from 'react';
import { WebSocketState } from '@/types/websocket';
import { wsManager } from '@/services/websocket/WebSocketManager';
import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';

export function useWebSocketState(selectedModel: string) {
  const [wsState, setWsState] = useState<WebSocketState>({
    isConnected: false,
    clientId: null,
    messages: [],
    streamTokens: []
  });
  const [liveStreamContent, setLiveStreamContent] = useState('');

  const handleClientId = useCallback((clientId: string) => {
    setWsState(prev => ({
      ...prev,
      isConnected: true,
      clientId
    }));
  }, []);

  const handleGenericMessage = useCallback((message: any) => {
    if (message.messageType === 'conversation') {
      const state = store.getState();
      const activeConversationId = state.conversations.activeConversationId;

      store.dispatch(addMessage({
        conversationId: activeConversationId,
        message: message.content
      }));
    }

    setWsState(prev => ({
      ...prev,
      messages: [...prev.messages, JSON.stringify(message)]
    }));
  }, []);

  const handleNewStreamToken = useCallback((token: string) => {
    setLiveStreamContent(wsManager.getStreamTokens());
    setWsState(prev => ({
      ...prev,
      streamTokens: [...prev.streamTokens, { token, timestamp: Date.now() }]
    }));
  }, []);

  const handleEndStream = useCallback(() => {
    setLiveStreamContent('');
  }, []);

  useEffect(() => {
    if (selectedModel !== "Select Model") {
      wsManager.connect();
      return () => {
        wsManager.disconnect();
        setWsState(prev => ({
          ...prev,
          isConnected: false,
          clientId: null
        }));
        setLiveStreamContent('');
      };
    }
  }, [selectedModel]);

  return {
    wsState,
    liveStreamContent,
    handleClientId,
    handleGenericMessage,
    handleNewStreamToken,
    handleEndStream
  };
}