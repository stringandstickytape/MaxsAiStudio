// src/hooks/useStreamTokens.ts
import { useState, useEffect, useCallback } from 'react';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

/**
 * Hook for working with streaming tokens from the WebSocket
 */
export function useStreamTokens() {
    const [streamTokens, setStreamTokens] = useState<string[]>([]);
    
    useEffect(() => {
        // Listen for new token fragments
        const unsubscribeToken = listenToWebSocketEvent('stream:token', (detail) => {
            setStreamTokens(prevTokens => [...prevTokens, detail.content]);
        });
        
        // Listen for end stream events
        const unsubscribeEnd = listenToWebSocketEvent('stream:end', () => {
            setStreamTokens([]);
        });
        
        return () => {
            unsubscribeToken();
            unsubscribeEnd();
        };
    }, []);

    return { streamTokens };
}