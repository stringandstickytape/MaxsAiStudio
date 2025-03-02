import { useState, useEffect, useCallback } from 'react';
import { WebSocketState } from '@/types/websocket';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useWebSocket } from './useWebSocket';

// This is a compatibility wrapper to maintain backward compatibility
// with code that uses the old useWebSocketState hook
export function useWebSocketState() {
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: webSocketService.isConnected(),
        clientId: webSocketService.getClientId(),
        messages: []
    });
    
    const { isConnected, clientId } = useWebSocket({
        autoConnect: true
    });
    
    // Keep wsState in sync with WebSocketService
    useEffect(() => {
        setWsState(prev => ({
            ...prev,
            isConnected,
            clientId
        }));
    }, [isConnected, clientId]);
    
    // These methods are kept for backward compatibility
    const handleClientId = useCallback((clientId: string) => {
        setWsState(prev => ({ ...prev, isConnected: true, clientId }));
    }, []);
    
    const handleGenericMessage = useCallback((message: any) => {
        setWsState(prev => ({
            ...prev,
            messages: [...prev.messages, JSON.stringify(message)]
        }));
    }, []);
    
    return {
        wsState,
        handleClientId,
        handleGenericMessage
    };
}