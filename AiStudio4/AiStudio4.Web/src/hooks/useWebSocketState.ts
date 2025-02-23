import { useState, useEffect, useCallback } from 'react';
import { WebSocketState } from '@/types/websocket';
import { messageService } from '@/services/messaging/WebSocketMessageService';
import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';

export function useWebSocketState() {
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: messageService.isConnected(),
        clientId: null, // Initially null; will be set via connection event.
        messages: [],
        streamTokens: [] //This can probably be removed as per earlier comment.
    });

    const handleClientId = useCallback((clientId: string) => {
        setWsState(prev => ({ ...prev, isConnected: true, clientId }));
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


    useEffect(() => {
        const handleConnectionStatus = (status: { isConnected: boolean, clientId?: string }) => {
             setWsState(prev => ({
                 ...prev,
                 isConnected: status.isConnected,
                 clientId: status.clientId || prev.clientId
             }));
        };
        messageService.subscribe('connectionStatus', handleConnectionStatus);
        messageService.connect();

        return () => {
            messageService.unsubscribe('connectionStatus', handleConnectionStatus);
            messageService.disconnect();
            //Don't need to set wsState here - messageService.disconnect will cause connectionStatus to be emitted.
        };
    }, []);

    return {
        wsState,
        handleClientId,
        handleGenericMessage
    };
}