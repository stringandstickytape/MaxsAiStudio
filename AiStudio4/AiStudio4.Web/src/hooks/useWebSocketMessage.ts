// src/hooks/useWebSocketMessage.ts
import { useEffect } from 'react';
import { wsManager } from '@/services/websocket/WebSocketManager';

export const useWebSocketMessage = (
    messageType: string,
    handler: (data: any) => void
) => {
    useEffect(() => {
        wsManager.subscribe(messageType, handler);

        // Cleanup subscription when component unmounts
        return () => {
            wsManager.unsubscribe(messageType, handler);
        };
    }, [messageType, handler]);
};