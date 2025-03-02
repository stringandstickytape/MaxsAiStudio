import { useState, useEffect, useCallback } from 'react';
import { webSocketService } from '@/services/websocket/WebSocketService';

/**
 * Hook for working with streaming tokens from the WebSocket
 */
export function useStreamTokens() {
    const [streamTokens, setStreamTokens] = useState<string[]>([]);

    const handleNewStreamToken = useCallback((token: string) => {
        setStreamTokens(prevTokens => [...prevTokens, token]);
    }, []);

    const handleEndStream = useCallback(() => {
        setStreamTokens([]);
    }, []);

    useEffect(() => {
        webSocketService.subscribe('cfrag', handleNewStreamToken);
        webSocketService.subscribe('endstream', handleEndStream);

        return () => {
            webSocketService.unsubscribe('cfrag', handleNewStreamToken);
            webSocketService.unsubscribe('endstream', handleEndStream);
        };
    }, [handleNewStreamToken, handleEndStream]);

    return { streamTokens };
}