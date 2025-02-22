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
        // Concatenate the new token to the previous liveStreamContent instead of replacing it
        setLiveStreamContent(prev => prev + token);

        setWsState(prev => ({
            ...prev,
            streamTokens: [...prev.streamTokens, { token, timestamp: Date.now() }]
        }));
    }, []);

    const handleEndStream = useCallback(() => {
        setLiveStreamContent('');
    }, []);

    // Subscribe to events via the messageService
    useEffect(() => {
        messageService.subscribe('cfrag', handleNewStreamToken);
        return () => {
            messageService.unsubscribe('cfrag', handleNewStreamToken);
        };
    }, [handleNewStreamToken]);

    useEffect(() => {
        console.log('Setting up endstream subscription');
        messageService.subscribe('endstream', handleEndStream);
        return () => {
            console.log('Cleaning up endstream subscription');
            messageService.unsubscribe('endstream', handleEndStream);
        };
    }, [handleEndStream]);

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
            setWsState(prev => ({
                ...prev,
                isConnected: false,
                clientId: null
            }));
            setLiveStreamContent('');
        };
    }, []);

    return {
        wsState,
        liveStreamContent,
        handleClientId,
        handleGenericMessage,
        handleNewStreamToken,
        handleEndStream
    };
}