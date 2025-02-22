import { useState, useEffect, useCallback } from 'react';
import { WebSocketState } from '@/types/websocket';
import { wsManager } from '@/services/websocket/WebSocketManager';
import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';

export function useWebSocketState(selectedModel: string) {
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: wsManager.isConnected(),
        clientId: wsManager.getClientId() || null,
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
        const streamContent = wsManager.getStreamTokens();
        setLiveStreamContent(streamContent);

        setWsState(prev => ({
            ...prev,
            streamTokens: [...prev.streamTokens, { token, timestamp: Date.now() }]
        }));
    }, []);

    const handleEndStream = useCallback(() => {
        setLiveStreamContent('');
    }, []);

    // Subscribe to cfrag messages
    useEffect(() => {
        wsManager.subscribe('cfrag', handleNewStreamToken);
        return () => {
            wsManager.unsubscribe('cfrag', handleNewStreamToken);
        };
    }, [handleNewStreamToken]);

    // Subscribe to endstream messages
    useEffect(() => {
        console.log('Setting up endstream subscription');
        wsManager.subscribe('endstream', handleEndStream);
        return () => {
            console.log('Cleaning up endstream subscription');
            wsManager.unsubscribe('endstream', handleEndStream);
        };
    }, [handleEndStream]);

    // Handle WebSocket connection based on selected model and track connection status
    useEffect(() => {
        if (selectedModel !== "Select Model") {
            const handleConnectionStatus = (status: { isConnected: boolean }) => {
                setWsState(prev => ({
                    ...prev,
                    isConnected: status.isConnected,
                    clientId: wsManager.getClientId() || null
                }));
            };

            wsManager.subscribe('connectionStatus', handleConnectionStatus);
            wsManager.connect();

            return () => {
                wsManager.unsubscribe('connectionStatus', handleConnectionStatus);
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